# 32-Pin Board — Theory of Operation & Assembly Guide

> [!WARNING]
> **Experimental Pre-Alpha (v0.2):** The YM-IOA bank selection design has not been tested on physical hardware yet and is considered experimental pre-alpha. Use at your own risk.

This document covers the **32-pin ROM board** (`pcb/32pin.circuit.tsx`): a single-YM2149, bank-switched EPROM design with a native DIP-32 socket (128KB–512KB). For the shared memory map and pinout references common to all board variants, see [Hardware.md](Hardware.md). For the smaller, jumper-configured board, see [Hardware-28pin.md](Hardware-28pin.md).

> [!CAUTION]
> The dual-YM design (`pcb/32pin-max.circuit.tsx`) is not ready, should not be used, and may never be finished or supported.

## BOM Cost Estimation (Per Unit)

| Component | Part | Estimated Cost | Notes |
| :--- | :--- | :--- | :--- |
| **YM2149 / AY-3-8910 Clone** | U_YM | $2.00 | PSG (KC89C72) |
| **ATF16V8B (Logic)** | U_GAL | ~$0.60 | 20-pin PLD, same part as the 28-pin board |
| **AT27C010/020/040 (EPROM)** | U_ROM | $2.00 | 128KB / 256KB / 512KB; native DIP-32 socket |
| **74HCT373 (Octal Latch)** | U_LATCH | $0.40 | Data bus isolation |
| **LM358 (Op-Amp)** | U_AMP | $0.10 | Summing amp for audio out |
| **Passives (R/C)** | — | $0.30 | Reset network, audio mixing, decoupling, 4× bank pull-ups |
| **Total (Excl. PCB)** | | **~$5.40** | |

## Logic Compilation (ATF16V8B)

The cartridge uses the same programmable logic device as the 28-pin board (**ATF16V8B**, a 20-pin PLD) for address decoding only — ROM bank selection is handled by direct wiring from the YM2149's IOA port (see below), not by the PLD.

This project uses [**galette**](https://github.com/simon-frankau/galette), an open-source logic assembler. To compile into JEDEC files:

```bash
make logic
```

The source is `pld/rom_ym_32pin.pld`. It differs from the 28-pin PLD in one equation only: `/ROM_CE = A15 * RW` (32KB read window at $8000–$FFFF; no $4000–$7FFF window, no solder jumpers).

## Hardware Wiring

### 1. ATF16V8B Pinout (`U_GAL`)

| Pin | Signal | Source / Destination |
| :--- | :--- | :--- |
| 1 | NC | — |
| 2 | A15 | 7800 Address Bus |
| 3 | A14 | 7800 Address Bus |
| 4 | A0 | 7800 Address Bus |
| 5 | HALT | 7800 Maria Halt Signal |
| 6 | R/W | 7800 CPU R/W Line |
| 7 | PHI2 | 7800 CPU Clock (Cart Pin 32) |
| 8 | A13 | 7800 Address Bus |
| 9 | A12 | 7800 Address Bus |
| 10 | GND | Ground |
| 11 | A11 | 7800 Address Bus |
| 12–14 | NC | — |
| 15 | **YM_LE** | Latch Enable → 74HCT373 Pin 11 |
| 16 | **PHI2OUT** | Buffered Clock → U_YM Pin 22 |
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
| 11 | LE | PLD Pin 15 (YM_LE) |
| 20 | VCC | +5V |
| 10 | GND | Ground |

### 3. AT27C010/020/040 ROM (Native DIP-32 Socket)

The board carries a native 32-pin DIP-32 (600mil) socket compatible with the AT27C010 (128KB), AT27C020 (256KB), and AT27C040 (512KB) EPROM family. All three share the same pinout; only pins 30–31 differ in function across devices. The upper address lines (bank bits) come **directly from the YM2149's IOA port**, each with a 10kΩ pull-up to VCC.

#### AT27C010 Pinout

| Pin (Left) | Signal | Pin (Right) | Signal |
| :--- | :--- | :--- | :--- |
| **1** | VPP (tied VCC) | **32** | VCC (+5V) |
| **2** | A16 ← YM IOA1 | **31** | A18/PGM ← YM IOA3 |
| **3** | A15 ← YM IOA0 | **30** | A17 ← YM IOA2 |
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

> **Pin 30 (A17):** address input on AT27C020/040; no-connect on AT27C010.
> **Pin 31 (A18/PGM):** address input on AT27C040; PGM (program enable, must be high for read) on AT27C010/020 — satisfied automatically by the IOA3 pull-up.

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

#### IOA Port — Bank Switching

Software writes YM **register 14** (Port A) to drive the ROM's upper address lines directly. Register 7 must configure IOA as output before use. Each bank line has a **10kΩ pull-up** (`R_BANK0–3`) to VCC.

| YM Pin | Signal | Function |
| :--- | :--- | :--- |
| 21 | IOA0 | ROM A15 (bank bit 0) → U_ROM Pin 3 |
| 20 | IOA1 | ROM A16 (bank bit 1) → U_ROM Pin 2 |
| 19 | IOA2 | ROM A17 (bank bit 2) → U_ROM Pin 30 |
| 18 | IOA3 | ROM A18 (bank bit 3) → U_ROM Pin 31 |
| 17–14 | IOA4–IOA7 | Unassigned |

### 5. YM-IOA Bank Switching

> [!NOTE]
> This bank switching scheme (version 0.2) is currently experimental pre-alpha and has not yet been tested on physical hardware.

The full $8000–$FFFF ROM window (32KB) is swapped at once — there is no fixed region. Software protocol:

```
Write YM reg 7  (Mixer/IO):  bit 6 = 1 (IOA = output) — preserve mixer bits 0-5!
Write YM reg 14 (Port A):    bits[3:0] = bank number (A18:A15)
```

> **Register 7 discipline:** bit 6 of register 7 shares the register with the tone/noise mixer. Every mixer write in the player must OR in `$40` or the bank latch reverts to input mode (floating → pull-ups → top bank).

Supported configurations (32KB banks):

| ROM part | Capacity | Banks |
| :--- | :--- | :--- |
| AT27C010 | 128KB | 4 × 32KB via IOA0–1 |
| AT27C020 | 256KB | 8 × 32KB via IOA0–2 |
| AT27C040 | 512KB | 16 × 32KB via IOA0–3 |

**Power-on behaviour:** at reset the YM's IOA port is in input mode (Hi-Z), so the pull-ups select the **top bank** (bank 3 / 7 / 15 respectively). The cartridge header, reset vectors, and boot code must therefore live in the top bank. Because the whole window swaps, the bank-switch routine (and any code that runs across a switch, plus the interrupt/reset vectors) must be duplicated at the same offset in every bank — or run from console RAM.

### 6. Hardware Reset Logic (Warm Start Fix)

An RC network on YM **Pin 23 (!RESET)** prevents the warm-start stuck-tone issue caused by a quick power cycle bypassing BIOS delays.

1. **10kΩ pull-up:** VCC → Pin 23 (!RESET)
2. **10µF polarised cap:** (+) to Pin 23 (!RESET), (−) to GND

At power-up the discharged cap holds !RESET low for ~100ms while the +5V rail stabilises, then releases it high.

### 7. LM358 Audio Stage (U_AMP — Inverting Summing Amp)

YM channels A, B, C each pass through a **1kΩ isolation resistor** to a shared **SUM_NODE**.

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
