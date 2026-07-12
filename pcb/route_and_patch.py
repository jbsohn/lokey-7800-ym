#!/usr/bin/env python3
import json
import os
import re
import shutil
import subprocess
import sys
import pcbnew

# Change to the script's directory (pcb/)
os.chdir(os.path.dirname(os.path.abspath(__file__)))

BUILD_DIR = "./build/"
PCB_PATH = BUILD_DIR + "KiCad/index.kicad_pcb"
DSN_PATH = BUILD_DIR + "KiCad/index.dsn"
SES_PATH = BUILD_DIR + "KiCad/index.ses"
GERBER_DIR = BUILD_DIR + "gerbers/"
PRO_PATH = BUILD_DIR + "KiCad/index.kicad_pro"
DRU_PATH = BUILD_DIR + "KiCad/index.kicad_dru"
DRC_RPT_PATH = BUILD_DIR + "index-drc.rpt"
GERBER_ZIP_PATH = BUILD_DIR + "gerbers"

ENTRY_FILE = sys.argv[1] if len(sys.argv) > 1 else "index.circuit.tsx"
BOARD = os.path.basename(ENTRY_FILE).split(".")[0]

print(f"Exporting unrouted board from tscircuit React ({ENTRY_FILE})...")
os.makedirs(os.path.dirname(PCB_PATH), exist_ok=True)
subprocess.run(
    ["npx", "tsci", "export", ENTRY_FILE, "-f", "kicad_pcb", "-o", PCB_PATH],
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
    if "rule_severities" not in (pro.get("board", {}).get("design_settings", {})):
        print("Error: index.kicad_pro is missing board.design_settings.rule_severities")
        sys.exit(1)
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

# HALT, PHI2, RW, A13, A14, VCC, GND must escape through the narrow connector notch.
(rule "connector_notch_escape_clearance"
  (constraint edge_clearance (min 0mm))
  (condition "A.NetName == 'HALT' || B.NetName == 'HALT' || A.NetName == 'PHI2' || B.NetName == 'PHI2' || A.NetName == 'RW' || B.NetName == 'RW' || A.NetName == 'A13' || B.NetName == 'A13' || A.NetName == 'A14' || B.NetName == 'A14' || A.NetName == 'VCC' || B.NetName == 'VCC' || A.NetName == 'GND' || B.NetName == 'GND'")
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
else:
    print("Warning: global rule block not found in DSN — edge clearance patch skipped")

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
else:
    print("Warning: class rule block not found in DSN — edge clearance patch skipped")

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

# The documented way to run freerouting is `java -jar freerouting-X.Y.Z.jar`
# (see https://github.com/freerouting/freerouting/blob/master/docs/command_line_arguments.md).
# Prefer that on every platform (including macOS) if FREEROUTING_JAR points at
# a jar; otherwise fall back to a `freerouting` executable/wrapper on PATH
# (e.g. a distro package).
freerouting_jar = os.getenv("FREEROUTING_JAR")
if freerouting_jar:
    if not os.path.exists(freerouting_jar):
        print(f"Error: FREEROUTING_JAR is set but file not found: {freerouting_jar}")
        sys.exit(1)
    freerouting_cmd = ["java", "-Djava.awt.headless=true", "-jar", freerouting_jar]
else:
    freerouting_bin = os.getenv("FREEROUTING_BIN", "freerouting")
    if not shutil.which(freerouting_bin) and not os.path.exists(freerouting_bin):
        print(f"Error: freerouting executable not found in PATH ({freerouting_bin}).")
        print(
            "Set FREEROUTING_JAR to a freerouting-*.jar path (run via `java -jar`),"
            " or FREEROUTING_BIN to an executable."
        )
        sys.exit(1)
    freerouting_cmd = [freerouting_bin]

freerouting_proc = subprocess.Popen(
    freerouting_cmd
    + [
        "-de",
        DSN_PATH,
        "-do",
        SES_PATH,
        "-mp",
        "0",
        "-oit",
        "0",
        "--gui.enabled=false",
    ],
    stdout=subprocess.PIPE,
    stderr=subprocess.STDOUT,
    text=True,
)
freerouting_output = []
for line in freerouting_proc.stdout:
    print(line, end="")
    freerouting_output.append(line)
freerouting_proc.wait()
freerouting_output = "".join(freerouting_output)

if freerouting_proc.returncode != 0:
    print(f"Error: freerouting exited with code {freerouting_proc.returncode}.")
    sys.exit(1)

session_match = re.search(r"session completed:.*", freerouting_output)
if session_match is None:
    print(
        "Error: could not find a freerouting session completion summary in the"
        " output (see above). Refusing to import/export an unverified board."
    )
    sys.exit(1)
unrouted_match = re.search(r"\((\d+) unrouted\)", session_match.group(0))
unrouted_count = int(unrouted_match.group(1)) if unrouted_match else 0
if unrouted_count > 0:
    print(
        f"Error: freerouting finished with {unrouted_count} unrouted connection(s)"
        " (see 'session completed' summary above)."
        " Refusing to import/export a board with missing copper."
    )
    sys.exit(1)

if not os.path.exists(SES_PATH):
    print("Error: freerouting failed to generate session file.")
    sys.exit(1)

print("Importing session routing back into KiCad...")
board = pcbnew.LoadBoard(PCB_PATH)
pcbnew.ImportSpecctraSES(board, SES_PATH)

print("Refilling zones...")
filler = pcbnew.ZONE_FILLER(board)
filler.Fill(board.Zones())
board.Save(PCB_PATH)

print("Running DRC...")
subprocess.run(
    ["kicad-cli", "pcb", "drc", "-o", DRC_RPT_PATH, PCB_PATH],
    check=True,
)

# Freerouting's own "session completed ... (N unrouted)" self-report is not
# reliable on its own: it has been observed to claim 0 unrouted while the
# actually-imported board is missing copper on real signal nets. Cross-check
# against KiCad's own post-import DRC, which independently recomputes
# connectivity from the routed geometry. The one expected false positive is
# the right-shoulder GND zone-fill island (two GND copper zones reported as
# "unconnected" to each other; real GND connectivity is maintained through
# component leads) — everything else is a genuine missing-copper defect.
with open(DRC_RPT_PATH) as f:
    drc_lines = f.readlines()

real_unconnected = []
i = 0
while i < len(drc_lines):
    if drc_lines[i].strip().startswith("[unconnected_items]"):
        items = [
            line.strip()
            for line in drc_lines[i + 1 : i + 4]
            if line.strip().startswith("@(")
        ]
        if not all("Zone [GND]" in item for item in items):
            real_unconnected.append(items)
    i += 1

if real_unconnected:
    print(
        f"Error: DRC found {len(real_unconnected)} unconnected item(s) that are"
        " NOT the known GND zone-fill-island artifact — real missing copper:"
    )
    for items in real_unconnected:
        for item in items:
            print(f"    {item}")
    print(
        "Refusing to export Gerbers for a board with unrouted signals"
        f" (see full report: {DRC_RPT_PATH})."
    )
    sys.exit(1)
print("  DRC connectivity check passed (no real unconnected items)")

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
    gbrjob["GeneralSpecs"]["Finish"] = "ENIG"
    gbrjob["GeneralSpecs"]["ProjectId"]["Revision"] = "Rev1"
    with open(gbrjob_path, "w") as f:
        json.dump(gbrjob, f, indent=2)
    print("  Set Finish: ENIG, Revision: Rev1")
else:
    print("Warning: gbrjob not found")

print(f"Zipping Gerber files to {GERBER_ZIP_PATH}.zip...")
shutil.make_archive(GERBER_ZIP_PATH, "zip", GERBER_DIR)

# Board-specific copies so downstream targets (previews, CI artifacts) can
# never grab outputs from whichever board happened to build last.
print(f"Copying board-specific outputs for {BOARD}...")
shutil.copy(PCB_PATH, BUILD_DIR + f"index-{BOARD}.kicad_pcb")
shutil.copy(DRC_RPT_PATH, BUILD_DIR + f"index-{BOARD}-drc.rpt")
shutil.copy(GERBER_ZIP_PATH + ".zip", BUILD_DIR + f"gerbers-{BOARD}.zip")

print("\nSuccess! Fully routed KiCad PCB and Gerbers are updated.")
