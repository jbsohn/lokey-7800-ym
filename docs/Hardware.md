# Hardware Specification & Wiring

This document contains the technical hardware specifications, memory mapping, and pinout references shared across all board variants of the YM2149 Atari 7800 bridge.

## Board Variants

This project has two board designs, each with its own dedicated hardware doc for PLD/ROM wiring, jumpers, and BOM:

* **[28-Pin Board](Hardware-28pin.md)** (`pcb/28pin.circuit.tsx`) — single YM2149, ATF16V8B PLD, solder-jumper ROM size selection (16/32/64KB).
* **[32-Pin Board](Hardware-32pin.md)** (`pcb/32pin-max.circuit.tsx`) — ATF22V10 PLD, native DIP-32 socket with software bank switching (128KB–512KB), optional cascaded second YM2149.

Everything below this section — the memory map and connector/chip pinout references — applies to both boards.

## Memory Mapping

The YM2149 is mapped to two specific addresses:

| Address | Function |
| :--- | :--- |
| **$0800** | YM2149 Address Register (select internal register) |
| **$0801** | YM2149 Data Register (write value to selected register) |

This mapping follows the **"YM $800" / Pokey800-compatible** decoding scheme: the PLD decodes a single address bit (`A11`, with `A15-A12` all low) rather than mirroring the chip across a wide window. This frees the entire `$4000-$7FFF` cartridge ROM window for actual game data instead of write-only sound registers, at the cost of dropping compatibility with the older "classic POKEY@$4000" mapping used by games like **Ballblazer** and **Commando**.

### Write-Only Mirroring

The current logic implementation is gated by the `!RW` (Read/Write) line. This means the YM2149 is a "write-only" device at $0800. This allows other devices (like ROM or RAM) to reside at the same memory addresses for **read** operations without bus contention.

---

## Hardware Pinout Reference

### 7800 Cartridge Edge (32-Pin)

From [AtariHQ](https://atarihq.com/danb/7800cart/a7800cart.shtml):

| Pin (1–16) | Signal | Pin (32–17) | Signal |
| :--- | :--- | :--- | :--- |
| **1** | Read/Write (low=Write) | **32** | Phase 2 Clock |
| **2** | Halt | **31** | IRQ |
| **3** | D3 | **30** | Ground |
| **4** | D4 | **29** | D2 |
| **5** | D5 | **28** | D1 |
| **6** | D6 | **27** | D0 |
| **7** | D7 | **26** | A0 |
| **8** | A12 | **25** | A1 |
| **9** | A10 | **24** | A2 |
| **10** | A11 | **23** | A3 |
| **11** | A9 | **22** | A4 |
| **12** | A8 | **21** | A5 |
| **13** | +5V VDC | **20** | A6 |
| **14** | Ground | **19** | A7 |
| **15** | A13 | **18** | External Audio Input |
| **16** | A14 | **17** | A15 |

### AY-3-8910 / YM2149 PSG (40-Pin)

| Pin (Left) | Signal | Pin (Right) | Signal |
| :--- | :--- | :--- | :--- |
| **1** | VSS (Ground) | **40** | VCC (+5V) |
| **2** | N.C. | **39** | TEST 1 |
| **3** | Analog Channel B | **38** | Analog Channel C |
| **4** | Analog Channel A | **37** | DA0 |
| **5** | N.C. | **36** | DA1 |
| **6** | IOB7 | **35** | DA2 |
| **7** | IOB6 | **34** | DA3 |
| **8** | IOB5 | **33** | DA4 |
| **9** | IOB4 | **32** | DA5 |
| **10** | IOB3 | **31** | DA6 |
| **11** | IOB2 | **30** | DA7 |
| **12** | IOB1 | **29** | BC1 |
| **13** | IOB0 | **28** | BC2 (Tie High) |
| **14** | IOA7 | **27** | BDIR |
| **15** | IOA6 | **26** | N.C. |
| **16** | IOA5 | **25** | A8 (Tie High) |
| **17** | IOA4 | **24** | !A9 (Tie Low) |
| **18** | IOA3 | **23** | !RESET |
| **19** | IOA2 | **22** | CLOCK |
| **20** | IOA1 | **21** | IOA0 |
