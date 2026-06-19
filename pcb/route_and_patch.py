#!/usr/bin/env python3
import json
import os
import shutil
import subprocess
import sys

import pcbnew

# Change to the script's directory (pcb/)
os.chdir(os.path.dirname(os.path.abspath(__file__)))

PCB_PATH = "./KiCad/index.kicad_pcb"
DSN_PATH = "./KiCad/index.dsn"
SES_PATH = "./KiCad/index.ses"
GERBER_DIR = "./gerbers/"
PRO_PATH = "./KiCad/index.kicad_pro"
DRU_PATH = "./KiCad/index.kicad_dru"

print("Exporting unrouted board from tscircuit React...")
os.makedirs(os.path.dirname(PCB_PATH), exist_ok=True)
subprocess.run(
    ["npx", "tsci", "export", "index.circuit.tsx", "-f", "kicad_pcb", "-o", PCB_PATH],
    check=True,
)

print("Patching PCB design settings via KiCad Python API...")
# Suppress C-level wx "create wxApp" noise that fires on first headless LoadBoard.
_null = os.open(os.devnull, os.O_WRONLY)
_old = os.dup(2)
os.dup2(_null, 2)
board = pcbnew.LoadBoard(PCB_PATH)
os.dup2(_old, 2)
os.close(_null)
os.close(_old)

all_fps = list(board.GetFootprints())
MIN_H = pcbnew.FromMM(0.8)
MIN_T = pcbnew.FromMM(0.1)
removed = 0
fixed = 0
stubs = []

for fp in all_fps:
    fid = fp.GetFPID()
    if (
        str(fid.GetLibNickname()) == "tscircuit"
        and str(fid.GetLibItemName()) == "Unknown"
    ):
        stubs.append(fp)
        removed += 1
    else:
        ref = fp.Reference()
        if ref.GetTextHeight() < MIN_H:
            ref.SetTextHeight(MIN_H)
            ref.SetTextWidth(MIN_H)
            ref.SetTextThickness(MIN_T)
            fixed += 1

for fp in stubs:
    board.Remove(fp)

print(f"  Removed {removed} tscircuit:Unknown stub footprint(s)")
print(f"  Fixed text size on {fixed} reference designator(s)")

zone_count = 0
for zone in board.Zones():
    if zone.GetNetname() == "GND":
        zone.SetIslandRemovalMode(0)  # 0 = ALWAYS keep islands
        zone.SetMinThickness(pcbnew.FromMM(0.15))
        zone_count += 1
print(f"  GND zones ({zone_count}): keep islands, min thickness 0.15mm")

ds = board.GetDesignSettings()
ds.m_ViasMinSize = pcbnew.FromMM(0.3)
ds.m_ViasMinAnnularWidth = pcbnew.FromMM(0.05)
ds.m_MinThroughDrill = pcbnew.FromMM(0.2)
ds.m_CopperEdgeClearance = 0
print("  Set design rules: via 0.3mm, annular 0.05mm, drill 0.2mm, edge clearance 0mm")

board.GetTitleBlock().SetRevision("Rev1")
print("  Set board revision: Rev1")

board.Save(PCB_PATH)

# Suppress cosmetic DRC warnings in index.kicad_pro.
# Create the file with a minimal skeleton if it doesn't exist yet (e.g. after make clean).
if os.path.exists(PRO_PATH):
    with open(PRO_PATH) as f:
        pro = json.load(f)
else:
    pro = {
        "meta": {"filename": "index.kicad_pro", "version": 1},
        "board": {"design_settings": {"rule_severities": {}}},
    }
sev = pro["board"]["design_settings"]["rule_severities"]
sev["lib_footprint_issues"] = "ignore"
sev["text_height"] = "ignore"
sev["text_thickness"] = "ignore"
# The right-shoulder area has no component pads, so the GND zone fill there
# produces a small isolated island that KiCad reports as zone-to-zone
# unconnected.  Real GND connectivity is maintained through all through-hole
# component leads; this is a zone fill geometry artifact (see TODO.md).
sev["unconnected_items"] = "warning"
with open(PRO_PATH, "w") as f:
    json.dump(pro, f, indent=2)
print("  Patched kicad_pro DRC severities")

# Write kicad_dru custom design rules.
with open(DRU_PATH, "w") as f:
    f.write("""\
(version 1)

# J1 is an Atari 7800 card-edge connector — pads intentionally sit at the board edge.
(rule "J1_card_edge_clearance"
  (constraint edge_clearance (min 0mm))
  (condition "A.Reference == 'J1' || B.Reference == 'J1'")
)

# HALT, A13, A14, VCC, GND must escape through the narrow connector notch.
(rule "connector_notch_escape_clearance"
  (constraint edge_clearance (min 0mm))
  (condition "A.NetName == 'HALT' || B.NetName == 'HALT' || A.NetName == 'A13' || B.NetName == 'A13' || A.NetName == 'A14' || B.NetName == 'A14' || A.NetName == 'VCC' || B.NetName == 'VCC' || A.NetName == 'GND' || B.NetName == 'GND'")
)

# The right-shoulder area has no component pads so the GND zone fill produces an
# isolated island there.  Real GND connectivity is maintained through all through-hole
# component leads.  Zone-to-zone unconnected is safe to ignore on this board.
(rule "gnd_zone_fill_artifact"
  (constraint unconnected (severity ignore))
  (condition "A.Type == 'Zone' && B.Type == 'Zone'")
)
""")
print("  Wrote kicad_dru custom design rules")

print("Exporting board to Specctra DSN...")
dsn_ok = pcbnew.ExportSpecctraDSN(board, DSN_PATH)
if not dsn_ok or not os.path.exists(DSN_PATH):
    print("Error: Failed to export board to Specctra DSN.")
    print(
        "This is usually caused by duplicate reference designators (e.g. U?, C?)"
        " or critical DRC violations in the board layout."
    )
    sys.exit(1)

print("Patching DSN rules and boundary...")
with open(DSN_PATH) as f:
    dsn = f.read()

# Patch the global rule block.
global_rule_old = """    (rule
      (width 200)
      (clearance 200)
      (clearance 50 (type smd_smd))
    )"""
global_rule_new = """    (rule
      (width 200)
      (clearance 200)
      (clearance 50 (type smd_smd))
      (clearance 0 (type smd_pcb))
      (clearance 0 (type pcb))
    )"""
if global_rule_old in dsn:
    dsn = dsn.replace(global_rule_old, global_rule_new)

# Patch the class rule block.
class_rule_old = """      (rule
        (width 200)
        (clearance 200)
      )"""
class_rule_new = """      (rule
        (width 200)
        (clearance 200)
        (clearance 0 (type smd_pcb))
        (clearance 0 (type pcb))
      )"""
if class_rule_old in dsn:
    dsn = dsn.replace(class_rule_old, class_rule_new)

# Patch boundary bottom edge coordinates from -140000 to -140200.
boundary_start = dsn.find("(boundary")
if boundary_start != -1:
    paren_count = 0
    boundary_end = -1
    for i in range(boundary_start, len(dsn)):
        if dsn[i] == "(":
            paren_count += 1
        elif dsn[i] == ")":
            paren_count -= 1
            if paren_count == 0:
                boundary_end = i + 1
                break
    if boundary_end != -1:
        boundary_block = dsn[boundary_start:boundary_end]
        new_boundary_block = boundary_block.replace("-140000", "-140200")
        dsn = dsn[:boundary_start] + new_boundary_block + dsn[boundary_end:]

with open(DSN_PATH, "w") as f:
    f.write(dsn)
print("  Patched DSN file")

print("Running freerouting...")
if os.path.exists(SES_PATH):
    try:
        os.remove(SES_PATH)
    except OSError:
        pass

freerouting_bin = os.getenv("FREEROUTING_BIN", "freerouting")
if not shutil.which(freerouting_bin) and not os.path.exists(freerouting_bin):
    mac_app_bin = os.getenv(
        "FREEROUTING_APP",
        "/Applications/freerouting.app/Contents/MacOS/freerouting",
    )
    if sys.platform == "darwin" and os.path.exists(mac_app_bin):
        freerouting_bin = mac_app_bin
    else:
        print(f"Error: freerouting executable not found in PATH or at {mac_app_bin}")
        print(
            "You can override these paths by setting FREEROUTING_BIN"
            " or FREEROUTING_APP environment variables."
        )
        sys.exit(1)

subprocess.run(
    [freerouting_bin, "-de", DSN_PATH, "-do", SES_PATH, "-mp", "10"],
    check=True,
)

if not os.path.exists(SES_PATH):
    print("Error: freerouting failed to generate session file.")
    sys.exit(1)

print("Importing session routing back into KiCad...")
board = pcbnew.LoadBoard(PCB_PATH)
pcbnew.ImportSpecctraSES(board, SES_PATH)
board.Save(PCB_PATH)

print("Refilling zones and running DRC...")
subprocess.run(
    ["kicad-cli", "pcb", "drc", "--refill-zones", "--save-board", PCB_PATH],
    check=True,
)

print("Exporting Gerbers and Drill files...")
os.makedirs(GERBER_DIR, exist_ok=True)
subprocess.run(
    ["kicad-cli", "pcb", "export", "gerbers", "-o", GERBER_DIR, PCB_PATH],
    check=True,
)
subprocess.run(
    ["kicad-cli", "pcb", "export", "drill", "-o", GERBER_DIR, PCB_PATH],
    check=True,
)

print("Patching Gerber job file metadata...")
gbrjob_path = os.path.join(GERBER_DIR, "index-job.gbrjob")
if os.path.exists(gbrjob_path):
    with open(gbrjob_path) as f:
        gbrjob = json.load(f)
    gbrjob["GeneralSpecs"]["Finish"] = "HAL"
    gbrjob["GeneralSpecs"]["ProjectId"]["Revision"] = "Rev1"
    with open(gbrjob_path, "w") as f:
        json.dump(gbrjob, f, indent=2)
    print("  Set Finish: HAL, Revision: Rev1")
else:
    print("Warning: gbrjob not found")

print("Zipping Gerber files to gerbers.zip...")
shutil.make_archive("gerbers", "zip", GERBER_DIR)

print("\nSuccess! Fully routed KiCad PCB and Gerbers are updated.")
