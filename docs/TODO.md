# Project TODO

## Features

- **POKEY@$800 mapping** — implement alternative memory mapping at `$0800` (POKEY-compatible) for the YM2149.

## PCB Design

Remaining issues from the PCB design review.

### High

- **Bulk decoupling capacitor** — no 10–100µF electrolytic near power entry (VCC/GND). Standard practice for cartridge designs.
- **Decoupling cap placement** — C1–C4 and CBYPASS are 15–20mm from their respective ICs. Move closer to VCC pins for better noise immunity.

### Medium

- **Mounting holes** — cartridge PCB has no mounting holes for standoffs/screws inside the shell.
- **Silkscreen overlap** — `"Lokey 7800 YM v0.1"` sits at Y=-30.25mm, which is inside the edge connector region and will be obscured inside the cartridge slot.

### Low

- **`patch_pcb.py` stderr suppression** — `os.dup2(_null, 2)` silences all stderr during `LoadBoard()`, hiding legitimate errors alongside wxWidgets noise.
- **`auto-route.mjs` DSN patching** — boundary string replacement tied to specific tscircuit export format; could break on tscircuit version updates.
- **`auto-route.mjs` no `freerouting` check** — script fails cryptically if `freerouting` binary is not installed.
- **No test points** — no dedicated test pads for PHI2, RESET, BDIR, BC1, or audio debugging.
