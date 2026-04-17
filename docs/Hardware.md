# Hardware Specification & Wiring

This document contains the technical hardware specifications, memory mapping, and wiring diagrams for the YM2149 Atari 7800 bridge.

## Memory Mapping & POKEY Compatibility

The YM2149 sound card is mapped to the **$4000–$7FFF** range (16 KB).

This mapping follows the historical precedent set by classic Atari 7800 games like **Ballblazer** and **Commando**, which mapped the **POKEY** sound chip to $4000. By mirroring this 16 KB "Sound Area," we ensure high compatibility with existing hardware designs.

### Write-Only Mirroring
The current GAL logic is gated by the `!RW` (Read/Write) line. This means the YM2149 is a "write-only" device at $4000. This allows other devices (like ROM or RAM) to reside at the same memory addresses for **read** operations without bus contention.

## Hardware Wiring

### 1. GAL16V8 Pinout (`rom_ym.pld`)

| Pin | Signal | Source |
| :--- | :--- | :--- |
| 2 | A15 | 7800 Address Bus |
| 3 | A14 | 7800 Address Bus |
| 4 | A0 | 7800 Address Bus |
| 5 | HALT | 7800 Maria Halt Signal |
| 6 | R/W | 7800 CPU R/W Line |
| 7 | PHI2 | 7800 CPU Clock (Pin 28 on Cart) |
| 15 | **YM_LE** | Latch Enable output to 74HCT373 Pin 11 |
| 16 | **PHI2OUT** | Buffered Clock to YM Pin 22 |
| 17 | **BC1** | Connect to YM Pin 29 |
| 18 | **BDIR** | Connect to YM Pin 27 |
| 19 | **!ROM_CE** | Connect to 27C256 ROM Pin 20 (~CE) |
| 20 | VCC | +5V |

### 2. 74HCT373 Octal Latch Connections

| Latch Pin | Signal | Connection | 27C256 ROM Pin |
| :--- | :--- | :--- | :--- |
| 1 | ~OE | Ground | - |
| 2 | Q0 | YM Pin 37 (DA0) | - |
| 3 | D0 | 7800 Data Bus D0 | Pin 11 (O0) |
| 4 | D1 | 7800 Data Bus D1 | Pin 12 (O1) |
| 11 | LE | GAL Pin 15 (**YM_LE**) | - |
| 19 | Q7 | YM Pin 30 (DA7) | - |

### 3. YM2149 / AY-3-8910 Connections

| YM Pin | Signal | Connection |
| :--- | :--- | :--- |
| 22 | CLOCK | **PHI2OUT (GAL Pin 16)** |
| 27 | BDIR | GAL Pin 18 |
| 29 | BC1 | GAL Pin 17 |
| 30–37 | DA7–DA0 | 74HCT373 Q7–Q0 |

## Hardware Pinout Reference

From [AtariHQ](https://atarihq.com/danb/7800cart/a7800cart.shtml):

### 7800 Cartridge Edge (32-Pin)

![Atari 7800 Cartridge Edge Pinout Diagram](7800-cart-pinout.jpg)

*Image credit: [Dan Boris / AtariHQ](https://atarihq.com/danb/7800cart/a7800cart.shtml) (Used for educational/reference purposes)*

| Pin (1–16) | Signal Description | Pin (32–17) | Signal Description |
| :--- | :--- | :--- | :--- |
| **1** | Read/Write (from 6502, low=Write) | **32** | Phase 2 Clock (from 6502) |
| **2** | Halt (to 6502) | **31** | IRQ (to 6502) |
| **3** | D3 (to/from 6502) | **30** | Ground |
| **4** | D4 (to/from 6502) | **29** | D2 (from 6502) |
| **5** | D5 (to/from 6502) | **28** | D1 (from 6502) |
| **6** | D6 (to/from 6502) | **27** | D0 (from 6502) |
| **7** | D7 (to/from 6502) | **26** | A0 (from 6502) |
| **8** | A12 (from 6502) | **25** | A1 (from 6502) |
| **9** | A10 (from 6502) | **24** | A2 (from 6502) |
| **10** | A11 (from 6502) | **23** | A3 (from 6502) |
| **11** | A9 (from 6502) | **22** | A4 (from 6502) |
| **12** | A8 (from 6502) | **21** | A5 (from 6502) |
| **13** | +5 VDC | **20** | A6 (from 6502) |
| **14** | Ground | **19** | A7 (from 6502) |
| **15** | A13 (from 6502) | **18** | External Audio Input |
| **16** | A14 (from 6502) | **17** | A15 (from 6502) |

**Backward Compatibility:** Pins 3–14 and 19–30 are identical to the Atari 2600 standard. This allows the 7800 to be physically backward compatible with 2600 cartridges. The remaining pins are specific to the 7800 and enable its expanded memory and sound capabilities.

### 27C256 EPROM (32KB ROM)

| Pin (Left) | Signal | Pin (Right) | Signal |
| :--- | :--- | :--- | :--- |
| **1** | VPP (12.5V or VCC) | **28** | VCC (+5V) |
| **2** | A12 (7800 Cart Pin 8) | **27** | A14 (7800 Cart Pin 16) |
| **3** | A7 (7800 Cart Pin 19) | **26** | A13 (7800 Cart Pin 15) |
| **4** | A6 (7800 Cart Pin 20) | **25** | A8 (7800 Cart Pin 12) |
| **5** | A5 (7800 Cart Pin 21) | **24** | A9 (7800 Cart Pin 11) |
| **6** | A4 (7800 Cart Pin 22) | **23** | A11 (7800 Cart Pin 10) |
| **7** | A3 (7800 Cart Pin 23) | **22** | !OE (Output Enable - Ground) |
| **8** | A2 (7800 Cart Pin 24) | **21** | A10 (7800 Cart Pin 9) |
| **9** | A1 (7800 Cart Pin 25) | **20** | !CE (Chip Enable - GAL Pin 19) |
| **10** | A0 (7800 Cart Pin 26) | **19** | Q7 (Data D7 - Latch Pin 18) |
| **11** | Q0 (Data D0 - Latch Pin 3) | **18** | Q6 (Data D6 - Latch Pin 17) |
| **12** | Q1 (Data D1 - Latch Pin 4) | **17** | Q5 (Data D5 - Latch Pin 14) |
| **13** | Q2 (Data D2 - Latch Pin 7) | **16** | Q4 (Data D4 - Latch Pin 13) |
| **14** | GND (Ground) | **15** | Q3 (Data D3 - Latch Pin 8) |

### AY-3-8910 / YM2149 PSG (40-Pin)

| Pin (Left) | Signal | Pin (Right) | Signal |
| :--- | :--- | :--- | :--- |
| **1** | VSS (Ground) | **40** | VCC (+5V) |
| **2** | N.C. | **39** | TEST 1 |
| **3** | Analog Channel B | **38** | Analog Channel C |
| **4** | Analog Channel A | **37** | DA0 (Data/Address Bit 0) |
| **5** | N.C. | **36** | DA1 (Data/Address Bit 1) |
| **6** | IOB7 | **35** | DA2 (Data/Address Bit 2) |
| **7** | IOB6 | **34** | DA3 (Data/Address Bit 3) |
| **8** | IOB5 | **33** | DA4 (Data/Address Bit 4) |
| **9** | IOB4 | **32** | DA5 (Data/Address Bit 5) |
| **10** | IOB3 | **31** | DA6 (Data/Address Bit 6) |
| **11** | IOB2 | **30** | DA7 (Data/Address Bit 7) |
| **12** | IOB1 | **29** | BC1 (Bus Control 1) |
| **13** | IOB0 | **28** | BC2 (Bus Control 2) |
| **14** | IOA7 | **27** | BDIR (Bus Direction) |
| **15** | IOA6 | **26** | SEL (YM2149 Clock Div Select) |
| **16** | IOA5 | **25** | A8 (Address 8 - Tie High) |
| **17** | IOA4 | **24** | !A9 (Address 9 - Tie Low) |
| **18** | IOA3 | **23** | !RESET (Reset - Tie High) |
| **19** | IOA2 | **22** | CLOCK (Master Clock Input) |
| **20** | IOA1 | **21** | IOA0 |

**Note:** On the YM2149, Pin 26 (**SEL**) acts as an internal clock divider. When tied **Low**, the master clock is divided by 2. When tied **High**, the clock is used as-is. On the original AY-3-8910, this pin is part of the bus control logic.


