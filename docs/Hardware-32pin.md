# 32-Pin Board — Theory of Operation & Assembly Guide

This document covers the **32-pin ROM board** (`pcb/32pin-max.circuit.tsx`): a bank-switched EPROM design with an optional second, cascaded YM2149. For the shared memory map and pinout references common to all board variants, see [Hardware.md](Hardware.md). For the simpler, jumper-configured single-YM board, see [Hardware-28pin.md](Hardware-28pin.md).

## BOM Cost Estimation (Per Unit)

| Component | Part | Estimated Cost | Notes |
| :--- | :--- | :--- | :--- |
| **YM2149 / AY-3-8910 Clone** | U_YM | $2.00 | Primary PSG |
| **YM2149 / AY-3-8910 Clone** | U_YM2 | $2.00 | Optional second PSG — **DNP** |
| **ATF22V10 (Logic)** | U_GAL | ~$1.00 | 24-pin PLD; handles address decode, bank switching, and dual-YM control |
| **AT27C010/020/040 (EPROM)** | U_ROM | $2.00 | 128KB / 256KB / 512KB; native DIP-32 socket |
| **74HCT373 (Octal Latch)** | U_LATCH | $0.40 | Data bus isolation |
| **LM358 (Op-Amp)** | U_AMP | $0.10 | Summing amp for audio out |
| **Passives (R/C)** | — | $0.25 | Reset network, audio mixing, decoupling |
| **Total (Excl. PCB)** | | **~$5.75** | ~$7.75 with U_YM2 populated |

> **62256 SRAM (`U_RAM`)** is present on the board footprint but is currently **unwired (work in progress)** — it has no net connections in `32pin-max.circuit.tsx` yet. Do not populate it until its intended function (e.g. save-state or expanded scratch memory) is designed and documented.

## Logic Compilation (ATF22V10)

The cartridge uses a programmable logic device (**ATF22V10**, a 24-pin PLD) to handle address decoding, ROM bank switching, and dual-YM bus control.

This project uses [**galette**](https://github.com/simon-frankau/galette), an open-source logic assembler. To compile into JEDEC files:

```bash
make logic
```

## Hardware Wiring

### 1. ATF22V10 Pinout (`U_GAL`)

Unlike the [28-pin board's ATF16V8B](Hardware-28pin.md#atf16v8b-pinout-u_gal), this PLD has enough macrocells to drive the ROM's upper address lines directly — **no solder jumpers are used** for ROM bank selection on this board.

| Pin | Signal | Source / Destination |
| :--- | :--- | :--- |
| 1 | HALT | 7800 Maria Halt Signal |
| 2 | A15 | 7800 Address Bus |
| 3 | A14 | 7800 Address Bus |
| 4 | A13 | 7800 Address Bus |
| 5 | A12 | 7800 Address Bus |
| 6 | A11 | 7800 Address Bus |
| 7 | A0 | 7800 Address Bus |
| 8 | R/W | 7800 CPU R/W Line |
| 9 | PHI2 | 7800 CPU Clock (Cart Pin 32) |
| 10 | IOA0 | U_YM Pin 21 (bank bit 0) |
| 11 | IOA1 | U_YM Pin 20 (bank bit 1) |
| 12 | IOA2 | U_YM Pin 19 (bank bit 2) |
| 13 | IOA3 | U_YM Pin 18 (bank bit 3) |
| 14 | GND | Ground |
| 15 | **YM_LE** | Latch Enable → 74HCT373 Pin 11 |
| 16 | **PHI2OUT** | Buffered Clock → U_YM Pin 22, U_YM2 Pin 22 |
| 17 | **BC1** | → U_YM Pin 29 |
| 18 | **BDIR** | → U_YM Pin 27 |
| 19 | **!ROM_CE** | → U_ROM Pin 22 (~CE) |
| 20 | **ROM_A15** | → U_ROM Pin 3 |
| 21 | **ROM_A16** | → U_ROM Pin 2 |
| 22 | **ROM_A17** | → U_ROM Pin 30 |
| 23 | **ROM_A18** | → U_ROM Pin 31 |
| 24 | VCC | +5V |

### 2. 74HCT373 Octal Latch Connections

| Latch Pin | Signal | Connection |
| :--- | :--- | :--- |
| 1 | ~OE | Ground |
| 2–9 | Q0–Q7 | U_YM DA0–DA7 |
| 3–18 | D0–D7 | 7800 Data Bus D0–D7 |
| 11 | LE | PLD Pin 15 (YM_LE) |
| 20 | VCC | +5V |
| 10 | GND | Ground |

### 3. AT27C010/020/040 ROM (Native DIP-32 Socket)

The board carries a native 32-pin DIP-32 (600mil) socket compatible with the AT27C010 (128KB), AT27C020 (256KB), and AT27C040 (512KB) EPROM family. All three share the same pinout; only pins 30–31 differ in function across devices.

#### AT27C010 Pinout

| Pin (Left) | Signal | Pin (Right) | Signal |
| :--- | :--- | :--- | :--- |
| **1** | VPP (tied VCC) | **32** | VCC (+5V) |
| **2** | A16 ← PLD ROM_A16 | **31** | A18/PGM ← PLD ROM_A18 |
| **3** | A15 ← PLD ROM_A15 | **30** | A17 ← PLD ROM_A17 |
| **4** | A12 | **29** | A14 |
| **5** | A7 | **28** | A13 |
| **6** | A6 | **27** | A8 |
| **7** | A5 | **26** | A9 |
| **8** | A4 | **25** | A11 |
| **9** | A3 | **24** | OE (tied GND) |
| **10** | A2 | **23** | A10 |
| **11** | A1 | **22** | ~CE ← PLD Pin 19 |
| **12** | A0 | **21** | D7 |
| **13** | D0 | **20** | D6 |
| **14** | D1 | **19** | D5 |
| **15** | D2 | **18** | D4 |
| **16** | GND | **17** | D3 |

> **Pin 30 (A17):** address input on AT27C020/040; unused on AT27C010 (tie PLD ROM_A17 low in software or leave bank bit 2 at 0).
> **Pin 31 (A18/PGM):** address input on AT27C040; PGM (program enable, VCC for read) on AT27C010/020.

### 4. YM2149 / AY-3-8910 Connections (U_YM)

#### Bus & Control

| YM Pin | Signal | Connection |
| :--- | :--- | :--- |
| 22 | CLOCK | PHI2OUT (PLD Pin 16) |
| 27 | BDIR | PLD Pin 18 |
| 29 | BC1 | PLD Pin 17 |
| 28 | BC2 | VCC |
| 25 | A8 | VCC |
| 24 | !A9 | GND |
| 23 | !RESET | RESET_DELAYED (RC network) |
| 30–37 | DA7–DA0 | 74HCT373 Q7–Q0 |

#### IOA Port — Bank Switching & YM2 Control

Software writes YM1 **register 14** (Port A) to drive ROM upper address lines and control U_YM2. Register 7 must configure IOA as output before use.

| YM Pin | Signal | Function |
| :--- | :--- | :--- |
| 21 | IOA0 | ROM A15 (bank bit 0) → PLD → U_ROM Pin 3 |
| 20 | IOA1 | ROM A16 (bank bit 1) → PLD → U_ROM Pin 2 |
| 19 | IOA2 | ROM A17 (bank bit 2) → PLD → U_ROM Pin 30 |
| 18 | IOA3 | ROM A18 (bank bit 3) → PLD → U_ROM Pin 31 |
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

| ROM part | Capacity | Banks |
| :--- | :--- | :--- |
| AT27C010 | 128KB | 2 × 64KB via IOA0 |
| AT27C020 | 256KB | 4 × 64KB via IOA0–1 |
| AT27C040 | 512KB | 8 × 64KB via IOA0–2 |
| (future, larger part) | 1MB+ | 16 × 64KB via IOA0–3 |

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
