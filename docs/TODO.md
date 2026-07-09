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


