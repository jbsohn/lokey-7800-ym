# 32-Pin Board — Theory of Operation & Assembly Guide

> [!WARNING]
> **Experimental Pre-Alpha (v0.3):** The fixed+banked YM-IOA bank selection design has not been tested on physical hardware yet and is considered experimental pre-alpha. Use at your own risk.

This document covers the **32-pin ROM board** (`pcb/32pin.circuit.tsx`): a single-YM2149, bank-switched EPROM design with a native DIP-32 socket. The memory layout is SuperGame-style: a **fixed 32KB code bank at $8000–$FFFF** and a **switched 16KB data window at $4000–$7FFF**, with the bank number held in the YM2149's IOA port and decoded by the ATF22V10 PLD. For the shared memory map and pinout references common to all board variants, see [Hardware.md](Hardware.md). For the smaller, jumper-configured board, see [Hardware-28pin.md](Hardware-28pin.md).

## BOM Cost Estimation (Per Unit)

| Component | Part | Estimated Cost | Notes |
| :--- | :--- | :--- | :--- |
| **YM2149 / AY-3-8910 Clone** | U_YM | $2.00 | PSG (KC89C72) |
| **ATF22V10 (Logic)** | U_GAL | ~$1.50 | 24-pin PLD: address decode + bank mapping |
| **AT27C010/020/040 (EPROM)** | U_ROM | $2.00 | 128KB / 256KB (512KB part usable, top half only); native DIP-32 socket |
| **74HCT373 (Octal Latch)** | U_LATCH | $0.40 | Data bus isolation |
| **LM358 (Op-Amp)** | U_AMP | $0.10 | Summing amp for audio out |
| **Passives (R/C)** | — | $0.30 | Reset network, audio mixing, decoupling, 4× bank pull-ups |
| **Total (Excl. PCB)** | | **~$6.30** | |

## Logic Compilation (ATF22V10)

Unlike the 28-pin board (ATF16V8B), this board uses an **ATF22V10** (24-pin PLD). It performs both address decoding *and* ROM bank mapping: the YM2149's IOA0–IOA3 pins feed the PLD, which generates the ROM's upper address lines A14–A17. Every pin of the PLD is used.

This project uses [**galette**](https://github.com/simon-frankau/galette), an open-source logic assembler. To compile into JEDEC files:

```bash
make logic
```

The source is `pld/rom_ym_32pin.pld`. Core equations:

```
/ROMCE  = A15 * RW  +  /A15 * A14 * RW     ; ROM window $4000–$FFFF, reads only
ROMA14  = A15 * A14 +  /A15 * IOA0         ; fixed: console A14; banked: bank bit 0
ROMA15  = A15 + IOA1                       ; fixed region forces the top of the chip
ROMA16  = A15 + IOA2
ROMA17  = A15 + IOA3
```

Because the fixed bank lives at the **top** of the EPROM (all upper address bits high), "force top bank" is a simple OR against A15 — no mux terms needed.

## Hardware Wiring

### 1. ATF22V10 Pinout (`U_GAL`)

| Pin | Signal | Source / Destination |
| :--- | :--- | :--- |
| 1 | PHI2 | 7800 CPU Clock (Cart Pin 32) |
| 2 | A15 | 7800 Address Bus |
| 3 | A14 | 7800 Address Bus |
| 4 | A13 | 7800 Address Bus |
| 5 | A12 | 7800 Address Bus |
| 6 | A11 | 7800 Address Bus |
| 7 | A0 | 7800 Address Bus |
| 8 | R/W | 7800 CPU R/W Line |
| 9 | HALT | 7800 Maria Halt Signal |
| 10 | IOA0 | ← U_YM Pin 21 (bank bit 0) |
| 11 | IOA1 | ← U_YM Pin 20 (bank bit 1) |
| 12 | GND | Ground |
| 13 | IOA2 | ← U_YM Pin 19 (bank bit 2) |
| 14 | IOA3 | ← U_YM Pin 18 (bank bit 3, used as input) |
| 15 | **ROM_A14** | → U_ROM Pin 29 (A14) |
| 16 | **ROM_A15** | → U_ROM Pin 3 (A15) |
| 17 | **ROM_A16** | → U_ROM Pin 2 (A16) |
| 18 | **ROM_A17** | → U_ROM Pin 30 (A17) |
| 19 | **!ROM_CE** | → U_ROM Pin 22 (~CE) |
| 20 | **BC1** | → U_YM Pin 29 |
| 21 | **BDIR** | → U_YM Pin 27 |
| 22 | **PHI2OUT** | Buffered Clock → U_YM Pin 22 |
| 23 | **YM_LE** | Latch Enable → 74HCT373 Pin 11 |
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

The board carries a native 32-pin DIP-32 (600mil) socket compatible with the AT27C010 (128KB), AT27C020 (256KB), and AT27C040 (512KB) EPROM family. All three share the same pinout; only pins 30–31 differ in function across devices. The upper address lines **A14–A17 come from the PLD** (which muxes between console A14 and the YM IOA bank bits); only A0–A13 come directly from the console bus.

#### AT27C010 Pinout

| Pin (Left) | Signal | Pin (Right) | Signal |
| :--- | :--- | :--- | :--- |
| **1** | VPP (tied VCC) | **32** | VCC (+5V) |
| **2** | A16 ← PLD Pin 17 | **31** | A18/PGM (tied VCC) |
| **3** | A15 ← PLD Pin 16 | **30** | A17 ← PLD Pin 18 |
| **4** | A12 | **29** | A14 ← PLD Pin 15 |
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
> **Pin 31 (A18/PGM):** on AT27C010/020 this is PGM (program enable, must be high for read) — tied to VCC. On AT27C040 it is A18, so tying it to VCC means the 512KB part exposes only its **top 256KB**; program images into the upper half.

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

#### IOA Port — Bank Register

The YM's IOA port holds the current bank number. Software writes YM **register 14** (Port A); register 7 must configure IOA as output before use. The four bank lines feed the **PLD** (not the ROM directly), each with a **10kΩ pull-up** (`R_BANK0–3`) to VCC.

| YM Pin | Signal | Function |
| :--- | :--- | :--- |
| 21 | IOA0 | Bank bit 0 → U_GAL Pin 10 |
| 20 | IOA1 | Bank bit 1 → U_GAL Pin 11 |
| 19 | IOA2 | Bank bit 2 → U_GAL Pin 13 |
| 18 | IOA3 | Bank bit 3 → U_GAL Pin 14 |
| 17–14 | IOA4–IOA7 | Unassigned |

### 5. Fixed + Banked Memory Layout

> [!NOTE]
> This bank switching scheme (version 0.3) is currently experimental pre-alpha and has not yet been tested on physical hardware.

SuperGame-style split, decoded entirely in the ATF22V10:

| Console address | Contents | ROM location |
| :--- | :--- | :--- |
| $8000–$FFFF | **Fixed 32KB** — player code, header, vectors | Top 32KB of the chip (always mapped, ignores IOA) |
| $4000–$7FFF | **Switched 16KB window** — song/level data | 16KB bank *N*, where *N* = IOA[3:0] |

Software protocol (unchanged from v0.2):

```
Write YM reg 7  (Mixer/IO):  bit 6 = 1 (IOA = output) — preserve mixer bits 0-5!
Write YM reg 14 (Port A):    bits[3:0] = bank number (16KB units)
```

> **Register 7 discipline:** bit 6 of register 7 shares the register with the tone/noise mixer. Every mixer write in the player must OR in `$40` or the bank register reverts to input mode (floating → pull-ups → top bank). With the fixed code bank this is no longer fatal — it only redirects the **data window** — but it will still corrupt whatever the player is streaming.

Supported configurations (16KB banks; the top two banks are the fixed 32KB region, visible in the data window only as mirrors):

| ROM part | Capacity | Data banks |
| :--- | :--- | :--- |
| AT27C010 | 128KB | 6 × 16KB (banks 0–5; IOA3 ignored) |
| AT27C020 | 256KB | 14 × 16KB (banks 0–13) |
| AT27C040 | 512KB (top half used) | 14 × 16KB (banks 0–13) |

**Power-on behaviour:** at reset the YM's IOA port is in input mode (Hi-Z), so the pull-ups select bank 15 and the data window shows a mirror of $C000–$FFFF — harmless. The reset vectors and boot code live in the **fixed** bank, which is always mapped, so **no code or vector duplication across banks is required**. Bank switching only ever moves the $4000–$7FFF data window.

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
