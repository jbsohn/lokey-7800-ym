# Project TODO

## Features

- [x] ~~**POKEY@$800 mapping** — implement alternative memory mapping at `$0800` (POKEY-compatible) for the YM2149.~~ *(Done: both 28-pin and 32-pin boards now decode the YM2149 at $0800/$0801 instead of $4000/$4001. See `gal/rom_ym_28pin.pld`, `gal/rom_ym_32pin.pld`, and [Hardware.md](Hardware.md#memory-mapping). Follow-up: the `a7800`/`js7800` emulator forks still hardcode $4000-range detection via the `.a78` header's bit-6 flag — see [Emulation.md](Emulation.md) — and need a corresponding update.)*

## PCB Design

Remaining issues from the PCB design review.

### High

- [ ] **Bulk decoupling capacitor** — no 10–100µF electrolytic near power entry (VCC/GND). Standard practice for cartridge designs.
- [x] ~~**Decoupling cap placement** — C1–C4 and CBYPASS are 15–20mm from their respective ICs. Move closer to VCC pins for better noise immunity.~~ *(Fixed/Invalid: Because the ICs are rotated 270°, their long axis lies along the X-axis. For example, U1 (DIP-28) extends to X=17.78mm, meaning C1 at X=20mm is physically only 2.2mm from the chip edge. All decoupling caps are within ~2mm of their respective IC pins).*

### Medium

- [ ] **Mounting holes** — cartridge PCB has no mounting holes for standoffs/screws inside the shell.
- [x] ~~**Silkscreen overlap** — `"Lokey 7800 YM v0.1"` sits at Y=-30.25mm, which is inside the edge connector region and will be obscured inside the cartridge slot.~~ *(Fixed: Moved to Y=36mm/33mm, near the top of the board).*

### Low

> Note: `patch_pcb.py` and `auto-route.mjs` were merged into a single `pcb/route_and_patch.py` during the project reorg. Items below reference the current file.

- [ ] **`route_and_patch.py` stderr suppression** — `os.dup2(_null, 2)` silences all stderr during `LoadBoard()`, hiding legitimate errors alongside wxWidgets noise. *(Carried over from `patch_pcb.py` unchanged — still a blanket suppression around the whole call, not just the wx noise.)*
- [ ] **`route_and_patch.py` DSN patching** — boundary string replacement tied to specific tscircuit export format; could break on tscircuit version updates. *(Partially improved: the DSN rule-block patches now check `if X in dsn` and print a warning instead of failing silently. The boundary-coordinate replace (`"-140000"` → `"-140200"`) is still a bare string match with no fallback/warning if the literal doesn't appear.)*
- [x] ~~**`route_and_patch.py` no `freerouting` check** — script fails cryptically if `freerouting` binary is not installed.~~ *(Fixed: explicit `shutil.which()`/`os.path.exists()` check before invoking freerouting, with a clear error message, macOS `.app` bundle fallback, and `FREEROUTING_BIN`/`FREEROUTING_APP` env var overrides.)*
- [ ] **No test points** — no dedicated test pads for PHI2, RESET, BDIR, BC1, or audio debugging. *(Confirmed still absent from both `28pin.circuit.tsx` and `32pin-max.circuit.tsx`.)*
- [ ] **Right-shoulder isolated GND island** — 2 suppressed `[unconnected_items]` warnings remain at `(31.8mm, ~-16.6mm)` (right shoulder zone-to-zone). The chamfer vertices were removed from the board outline (cleaner geometry), but the shoulder area has no component pads and freerouting cannot route a trace into it, so the zone fill cannot self-connect there. The copper is electrically harmless (no signal, not in any current path). Warnings are suppressed via `gnd_zone_fill_artifact` DRC rule. True fix would require a physical pad or connector footprint extension into the shoulder area. *(Confirmed still open — `route_and_patch.py` still writes this exact suppression rule.)*

## Future Upgrades: 64KB Bank-Switching Support

Plan to support true **64KB ROM bank-switching** in a future revision by routing a registered output from the ATF16V8B PLD to the ROM's high-order address pin (A15).

### Tasks
- **PCB Schematic & Netlist Changes**:
  - Connect PLD Pin 11 (`OE`) to `GND` (required for registered mode).
  - Connect PLD Pin 12 (`BANK`) to JP1 Pin 3 (Right pad), replacing the direct `A15` connection.
  - Update `JP1` silkscreen to label `L` as `VCC (16K/32K)` and `R` as `BANK (64K)`.
- **PLD Logic Upgrades**:
  - Convert PLD design to registered mode.
  - Implement logic to latch the state of address lines on write cycles to register bank changes (e.g., using `A0` to toggle the bank output pin 12 when writing to `$8000`).

