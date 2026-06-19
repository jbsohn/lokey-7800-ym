# Project TODO

## Features

- **POKEY@$800 mapping** — implement alternative memory mapping at `$0800` (POKEY-compatible) for the YM2149.

## PCB Design

Remaining issues from the PCB design review.

### High

- [ ] **Bulk decoupling capacitor** — no 10–100µF electrolytic near power entry (VCC/GND). Standard practice for cartridge designs.
- [x] ~~**Decoupling cap placement** — C1–C4 and CBYPASS are 15–20mm from their respective ICs. Move closer to VCC pins for better noise immunity.~~ *(Fixed/Invalid: Because the ICs are rotated 270°, their long axis lies along the X-axis. For example, U1 (DIP-28) extends to X=17.78mm, meaning C1 at X=20mm is physically only 2.2mm from the chip edge. All decoupling caps are within ~2mm of their respective IC pins).*

### Medium

- [ ] **Mounting holes** — cartridge PCB has no mounting holes for standoffs/screws inside the shell.
- [x] ~~**Silkscreen overlap** — `"Lokey 7800 YM v0.1"` sits at Y=-30.25mm, which is inside the edge connector region and will be obscured inside the cartridge slot.~~ *(Fixed: Moved to Y=36mm/33mm, near the top of the board).*

### Low

- [ ] **`patch_pcb.py` stderr suppression** — `os.dup2(_null, 2)` silences all stderr during `LoadBoard()`, hiding legitimate errors alongside wxWidgets noise.
- [ ] **`auto-route.mjs` DSN patching** — boundary string replacement tied to specific tscircuit export format; could break on tscircuit version updates.
- [ ] **`auto-route.mjs` no `freerouting` check** — script fails cryptically if `freerouting` binary is not installed.
- [ ] **No test points** — no dedicated test pads for PHI2, RESET, BDIR, BC1, or audio debugging.
- [ ] **Right-shoulder isolated GND island** — 2 suppressed `[unconnected_items]` warnings remain at `(31.8mm, ~-16.6mm)` (right shoulder zone-to-zone). The chamfer vertices were removed from the board outline (cleaner geometry), but the shoulder area has no component pads and freerouting cannot route a trace into it, so the zone fill cannot self-connect there. The copper is electrically harmless (no signal, not in any current path). Warnings are suppressed via `gnd_zone_fill_artifact` DRC rule. True fix would require a physical pad or connector footprint extension into the shoulder area.

## Future Upgrades: 64KB Bank-Switching Support

Plan to support true **64KB ROM bank-switching** in a future revision by routing a registered output from the ATF16V8B GAL to the ROM's high-order address pin (A15).

### Tasks
- **PCB Schematic & Netlist Changes**:
  - Connect GAL Pin 11 (`OE`) to `GND` (required for registered mode).
  - Connect GAL Pin 12 (`BANK`) to JP1 Pin 3 (Right pad), replacing the direct `A15` connection.
  - Update `JP1` silkscreen to label `L` as `VCC (16K/32K)` and `R` as `BANK (64K)`.
- **GAL Logic Upgrades**:
  - Convert GAL PLD design to registered mode.
  - Implement logic to latch the state of address lines on write cycles to register bank changes (e.g., using `A0` to toggle the bank output pin 12 when writing to `$8000`).

