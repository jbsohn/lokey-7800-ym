# Hardware Specification & Wiring

This document contains the technical hardware specifications, memory mapping, and wiring diagrams for the YM2149 Atari 7800 bridge.

## Memory Mapping

The YM2149 is mapped to two specific addresses:

| Address | Function |
| :--- | :--- |
| **$4000** | YM2149 Address Register (select internal register) |
| **$4001** | YM2149 Data Register (write value to selected register) |

This mapping follows the historical precedent set by classic Atari 7800 games like **Ballblazer** and **Commando**, which mapped the **POKEY** sound chip to $4000. By mirroring this 16 KB "Sound Area," we ensure high compatibility with existing hardware designs.

### Write-Only Mirroring

The current logic implementation is gated by the `!RW` (Read/Write) line. This means the YM2149 is a "write-only" device at $4000. This allows other devices (like ROM or RAM) to reside at the same memory addresses for **read** operations without bus contention.

## BOM Cost Estimation (Per Unit)

| Component | Part | Estimated Cost | Notes |
| :--- | :--- | :--- | :--- |
| **YM2149 / AY-3-8910 Clone** | U_YM | $2.00 | Primary PSG |
| **YM2149 / AY-3-8910 Clone** | U_YM2 | $2.00 | Optional second PSG — **DNP** |
| **ATF16V8B (Logic)** | U_GAL | $0.85 | Modern replacement for legacy GAL16V8 |
| **AT27C010/020/040 (EPROM)** | U_ROM | $2.00 | 128KB / 256KB / 512KB; DIP-32 socket |
| **74HCT373 (Octal Latch)** | U_LATCH | $0.40 | Data bus isolation |
| **LM358 (Op-Amp)** | U_AMP | $0.10 | Summing amp for audio out |
| **Passives (R/C)** | — | $0.20 | Reset network, audio mixing, decoupling |
| **Total (Excl. PCB)** | | **~$5.55** | ~$7.55 with U_YM2 populated |

> **ROM compatibility:** A 28-pin 27C256 (32KB) also fits the DIP-32 socket by inserting at the +2 offset (chip pin 1 → socket hole 3). Holes 1–2 and 31–32 remain empty. JP1–JP4 must be bridged to L (VCC) for 28-pin operation.

## Logic Compilation (ATF16V8B / GAL16V8)

The cartridge uses a programmable logic device (typically an **ATF16V8B** or legacy **GAL16V8**) to handle address decoding and bus control.

This project uses [**galette**](https://github.com/simon-frankau/galette), an open-source logic assembler. To compile into JEDEC files:

```bash
make logic
```

## Hardware Wiring

### 1. ATF16V8B Pinout (`rom_ym.pld`)

| Pin | Signal | Source / Destination |
| :--- | :--- | :--- |
| 2 | A15 | 7800 Address Bus |
| 3 | A14 | 7800 Address Bus |
| 4 | A0 | 7800 Address Bus |
| 5 | HALT | 7800 Maria Halt Signal |
| 6 | R/W | 7800 CPU R/W Line |
| 7 | PHI2 | 7800 CPU Clock (Cart Pin 32) |
| 15 | **YM_LE** | Latch Enable → 74HCT373 Pin 11 |
| 16 | **PHI2OUT** | Buffered Clock → U_YM Pin 22, U_YM2 Pin 22 |
| 17 | **BC1** | → U_YM Pin 29 |
| 18 | **BDIR** | → U_YM Pin 27 |
| 19 | **!ROM_CE** | → U_ROM Pin 22 (~CE) |
| 20 | VCC | +5V |

### 2. 74HCT373 Octal Latch Connections

| Latch Pin | Signal | Connection |
| :--- | :--- | :--- |
| 1 | ~OE | Ground |
| 2–9 | Q0–Q7 | U_YM DA0–DA7 |
| 3–18 | D0–D7 | 7800 Data Bus D0–D7 |
| 11 | LE | GAL Pin 15 (YM_LE) |
| 20 | VCC | +5V |
| 10 | GND | Ground |

### 3. AT27C010/020/040 ROM (DIP-32 Socket)

The board carries a 32-pin DIP-32 (600mil) socket compatible with the AT27C010 (128KB), AT27C020 (256KB), and AT27C040 (512KB) EPROM family. All three share the same pinout; only pins 30–31 differ in function across devices.

#### AT27C010 Pinout

| Pin (Left) | Signal | Pin (Right) | Signal |
| :--- | :--- | :--- | :--- |
| **1** | VPP (tied VCC) | **32** | VCC (+5V) |
| **2** | A16 → JP2 C | **31** | A18/PGM → JP4 C |
| **3** | A15 → JP1 C | **30** | A17 → JP3 C |
| **4** | A12 | **29** | A14 |
| **5** | A7 | **28** | A13 |
| **6** | A6 | **27** | A8 |
| **7** | A5 | **26** | A9 |
| **8** | A4 | **25** | A11 |
| **9** | A3 | **24** | OE (tied GND) |
| **10** | A2 | **23** | A10 |
| **11** | A1 | **22** | ~CE ← GAL Pin 19 |
| **12** | A0 | **21** | D7 |
| **13** | D0 | **20** | D6 |
| **14** | D1 | **19** | D5 |
| **15** | D2 | **18** | D4 |
| **16** | GND | **17** | D3 |

> **Pin 30 (A17):** NC on AT27C010; address input on AT27C020/040.
> **Pin 31 (A18/PGM):** PGM (program enable, tie VCC for read) on AT27C010/020; address input on AT27C040.

#### JP1–JP4 Solder Jumpers (ROM Size Selection)

| Jumper | Socket Pad | Bridge L | Bridge R | Purpose |
| :--- | :--- | :--- | :--- | :--- |
| **JP1** | Pin 3 (A15) | VCC | YM_IOA0 | 28-pin VPP fixed high; or 32-pin bank bit 0 |
| **JP2** | Pin 2 (A16) | GND | YM_IOA1 | Fixed A16=0 for 128K; or bank bit 1 |
| **JP3** | Pin 30 (A17) | VCC | YM_IOA2 | 28-pin NC/VCC; or bank bit 2 for 256K+ |
| **JP4** | Pin 31 (A18) | VCC | YM_IOA3 | PGM tied high for 010/020; or future bank bit 3 for 512K |

Bridge **L** for 28-pin 27C256 or fixed-bank 32-pin operation. Bridge **R** to enable YM-IOA bank switching for that address line.

### 4. YM2149 / AY-3-8910 Connections (U_YM)

#### Bus & Control

| YM Pin | Signal | Connection |
| :--- | :--- | :--- |
| 22 | CLOCK | PHI2OUT (GAL Pin 16) |
| 27 | BDIR | GAL Pin 18 |
| 29 | BC1 | GAL Pin 17 |
| 28 | BC2 | VCC |
| 25 | A8 | VCC |
| 24 | !A9 | GND |
| 23 | !RESET | RESET_DELAYED (RC network) |
| 30–37 | DA7–DA0 | 74HCT373 Q7–Q0 |

#### IOA Port — Bank Switching & YM2 Control

Software writes YM1 **register 14** (Port A) to drive ROM upper address lines and control U_YM2. Register 7 must configure IOA as output before use.

| YM Pin | Signal | Function |
| :--- | :--- | :--- |
| 21 | IOA0 | ROM A15 (bank bit 0) → JP1 R-pad |
| 20 | IOA1 | ROM A16 (bank bit 1) → JP2 R-pad |
| 19 | IOA2 | ROM A17 (bank bit 2) → JP3 R-pad |
| 18 | IOA3 | ROM A18 (bank bit 3, 512K future) → JP4 R-pad |
| 17 | IOA4 | Unassigned |
| 16 | IOA5 | Unassigned |
| 15 | IOA6 | U_YM2 BDIR |
| 14 | IOA7 | U_YM2 BC1 |

#### IOB Port — YM2 Data Bus

Software writes YM1 **register 15** (Port B) to set the data/address byte for U_YM2.

| YM Pin | Signal | Function |
| :--- | :--- | :--- |
| 13 | IOB0 | U_YM2 DA0 |
| 12 | IOB1 | U_YM2 DA1 |
| 11 | IOB2 | U_YM2 DA2 |
| 10 | IOB3 | U_YM2 DA3 |
| 9  | IOB4 | U_YM2 DA4 |
| 8  | IOB5 | U_YM2 DA5 |
| 7  | IOB6 | U_YM2 DA6 |
| 6  | IOB7 | U_YM2 DA7 |

### 5. YM-IOA Bank Switching

Software protocol for switching ROM banks (write to YM1 only):

```
Write YM reg 7  (Mixer/IO):  IOA = output, IOB = output
Write YM reg 14 (Port A):    bits[2:0] = bank number (A17:A15); bits[7:6] = BDIR/BC1 for YM2
```

Supported configurations:

| JP bridges | ROM part | Capacity | Banks |
| :--- | :--- | :--- | :--- |
| All L | 27C256 | 32KB | 1 (no switching) |
| JP1=R | AT27C010 | 128KB | 2 × 64KB via IOA0 |
| JP1–JP2=R | AT27C020 | 256KB | 4 × 64KB via IOA0–1 |
| JP1–JP3=R | AT27C040 | 512KB | 8 × 64KB via IOA0–2 |
| JP1–JP4=R | AT27C040+ | 1MB future | 16 × 64KB via IOA0–3 |

### 6. Optional Second YM2149 — U_YM2 (DNP)

U_YM2 is controlled entirely through U_YM1's IOA and IOB ports. It never touches the 7800 bus. Leave unpopulated for single-YM operation — no board changes required.

| U_YM2 Pin | Signal | Source |
| :--- | :--- | :--- |
| 22 | CLOCK | PHI2OUT (shared with U_YM) |
| 23 | !RESET | RESET_DELAYED (shared with U_YM) |
| 27 | BDIR | U_YM IOA6 |
| 29 | BC1 | U_YM IOA7 |
| 28 | BC2 | VCC |
| 25 | A8 | VCC |
| 24 | !A9 | GND |
| 37–30 | DA0–DA7 | U_YM IOB0–IOB7 |

#### Software Protocol for Writing to U_YM2

1. Write YM1 reg 15 (IOB) = YM2 **register number**
2. Write YM1 reg 14 (IOA) with bits 7:6 = `11` (BDIR=1, BC1=1) — YM2 latches address
3. Write YM1 reg 14 (IOA) with bits 7:6 = `00`
4. Write YM1 reg 15 (IOB) = **data value**
5. Write YM1 reg 14 (IOA) with bits 7:6 = `10` (BDIR=1, BC1=0) — YM2 writes data
6. Write YM1 reg 14 (IOA) with bits 7:6 = `00`

> **Bank bit preservation:** Steps 2, 3, 5, and 6 must OR the current bank bits into IOA bits[3:0] to avoid corrupting the active ROM bank during YM2 register writes.

### 7. Hardware Reset Logic (Warm Start Fix)

An RC network on YM **Pin 23 (!RESET)** prevents the warm-start stuck-tone issue caused by a quick power cycle bypassing BIOS delays.

1. **10kΩ pull-up:** VCC → Pin 23 (!RESET)
2. **10µF polarised cap:** (+) to Pin 23 (!RESET), (−) to GND

At power-up the discharged cap holds !RESET low for ~100ms while the +5V rail stabilises, then releases it high.

### 8. LM358 Audio Stage (U_AMP — Inverting Summing Amp)

YM channels A, B, C each pass through a **1kΩ isolation resistor** to a shared **SUM_NODE**. U_YM2 channels (when populated) add three more 1kΩ resistors to the same node.

| Pin | Signal | Connection |
| :--- | :--- | :--- |
| **1** | OUT1 | Op-amp output → R_FB → SUM_NODE (feedback) |
| **2** | IN1_NEG | SUM_NODE |
| **3** | IN1_POS | GND |
| **4** | GND | Ground Plane |
| **5** | IN2_POS | GND (unused channel stability) |
| **6** | IN2_NEG | OUT2 (unity-gain follower) |
| **7** | OUT2 | IN2_NEG (unity-gain follower) |
| **8** | VCC | +5V |

**Output path:** OUT1 → R_SERIES (1kΩ) → C_AUDIO_OUT (+) (10µF) → C_AUDIO_OUT (−) → **Exaudio (Cart Pin 18)**. The R_PULL (1kΩ) from OUT1 to GND biases the output stage into Class-A operation.

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
