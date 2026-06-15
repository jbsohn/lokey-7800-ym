#!/usr/bin/env python3
"""Post-export patches applied to the tscircuit KiCad output."""
import sys, os, json

import pcbnew

PCB_PATH = sys.argv[1]
KICAD_DIR = os.path.dirname(PCB_PATH)
PRO_PATH  = os.path.join(KICAD_DIR, "index.kicad_pro")
DRU_PATH  = os.path.join(KICAD_DIR, "index.kicad_dru")

# ── 1. PCB patches via pcbnew ─────────────────────────────────────────────────
# Suppress C-level wx "create wxApp" noise that fires on first headless LoadBoard
_null = os.open(os.devnull, os.O_WRONLY)
_old  = os.dup(2)
os.dup2(_null, 2)
board = pcbnew.LoadBoard(PCB_PATH)
os.dup2(_old, 2)
os.close(_null)
os.close(_old)

# Snapshot the footprint list once — avoid calling GetFootprints() after Remove()
# which can return stale SWIG objects in KiCad 10.
all_fps = list(board.GetFootprints())

MIN_H = pcbnew.FromMM(0.8)
MIN_T = pcbnew.FromMM(0.1)
removed = 0
fixed   = 0
stubs   = []

for fp in all_fps:
    fid = fp.GetFPID()
    if str(fid.GetLibNickname()) == "tscircuit" and str(fid.GetLibItemName()) == "Unknown":
        stubs.append(fp)
        removed += 1
    else:
        # Fix reference text sizes (min 0.8mm for silk DRC)
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

# Set GND zones to always remove isolated copper islands and allow narrow flow
for zone in board.Zones():
    if zone.GetNetname() == "GND":
        zone.SetIslandRemovalMode(0)  # 0 = ALWAYS
        zone.SetMinThickness(pcbnew.FromMM(0.15))
print("  Set GND zone island removal: ALWAYS, min thickness: 0.15mm")

board.Save(PCB_PATH)
print(f"  Saved {PCB_PATH}")

# ── 2. kicad_pro: suppress cosmetic DRC warnings ──────────────────────────────
if os.path.exists(PRO_PATH):
    with open(PRO_PATH) as f:
        pro = json.load(f)
    sev = pro["board"]["design_settings"]["rule_severities"]
    sev["lib_footprint_issues"] = "ignore"
    sev["text_height"]          = "ignore"
    sev["text_thickness"]       = "ignore"

    # Keep via minimums in sync with tscircuit's <via> defaults (0.3mm pad / 0.2mm drill)
    rules = pro["board"]["design_settings"]["rules"]
    rules["min_via_diameter"]          = 0.3
    rules["min_via_annular_width"]     = 0.05
    rules["min_through_hole_diameter"] = 0.2
    rules["min_copper_edge_clearance"] = 0.0
    with open(PRO_PATH, "w") as f:
        json.dump(pro, f, indent=2)
    print("  Patched kicad_pro DRC severities")
else:
    print("  WARNING: kicad_pro not found — run 'git checkout pcb/KiCad/index.kicad_pro'")

# ── 3. kicad_dru: custom design rules ────────────────────────────────────────
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
""")
print("  Wrote kicad_dru custom design rules")
