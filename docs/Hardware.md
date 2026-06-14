# Hardware Specification & Wiring

This document contains the technical hardware specifications, memory mapping, and wiring diagrams for the YM2149 Atari 7800 bridge.

## Memory Mapping & POKEY Compatibility

The YM2149 sound card is mapped to the **$4000–$7FFF** range (16 KB).

This mapping follows the historical precedent set by classic Atari 7800 games like **Ballblazer** and **Commando**, which mapped the **POKEY** sound chip to $4000. By mirroring this 16 KB "Sound Area," we ensure high compatibility with existing hardware designs.

### Write-Only Mirroring
The current logic implementation is gated by the `!RW` (Read/Write) line. This means the YM2149 is a "write-only" device at $4000. This allows other devices (like ROM or RAM) to reside at the same memory addresses for **read** operations without bus contention.

## BOM Cost Estimation (Per Unit)

The Lokey-YM is designed for high performance at a hobbyist-friendly price point.

| Component | Estimated Cost | Notes |
| :--- | :--- | :--- |
| **YM2149 / AY-3-8910 Clone** | $2.00 | Targeted price for bulk/clones |
| **ATF16V8B (Logic)** | $0.85 | Modern replacement for legacy GAL16V8 |
| **27C256 (32KB EPROM)** | $1.50 | Standard game ROM |
| **74HCT373 (Octal Latch)** | $0.40 | Address latching |
| **LM358 (Op-Amp)** | $0.10 | Active audio amplification |
| **Passives (R/C)** | $0.15 | Reset circuit and audio stage |
| **Total (Excl. PCB)** | **~$5.00** | |

## Logic Compilation (ATF16V8B / GAL16V8)

The cartridge uses a programmable logic device (typically an **ATF16V8B** or legacy **GAL16V8**) to handle the address decoding and bus control logic.

This project uses [**galette**](https://github.com/simon-frankau/galette), an open-source logic assembler. This allows for a modern, cross-platform toolchain without requiring legacy Windows-only tools. Original WinCUPL sources are preserved in `gal/wincupl/` as a reference.

To compile the logic into JEDEC files (for use with a device programmer):
```bash
make logic
```

## Hardware Wiring

### 1. ATF16V8B Pinout (`rom_ym.pld`)

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
| 11 | LE | Logic Pin 15 (**YM_LE**) | - |
| 19 | Q7 | YM Pin 30 (DA7) | - |

### 3. YM2149 / AY-3-8910 Connections

| YM Pin | Signal | Connection |
| :--- | :--- | :--- |
| 22 | CLOCK | **PHI2OUT (Logic Pin 16)** |
| 27 | BDIR | Logic Pin 18 |
| 29 | BC1 | Logic Pin 17 |
| 30–37 | DA7–DA0 | 74HCT373 Q7–Q0 |

### 4. Hardware Reset Logic (Warm Start Fix)

To prevent the YM2149 registers from retaining garbage data during a quick console power cycle ("Warm Start")—which bypasses default internal BIOS delays and causes a stuck, high-frequency tone—a dedicated hardware RC network and manual override switch are implemented on **Pin 23 (/RESET)**.

#### Connection & Wiring Guide
The components are tied together at a single electrical node connecting straight to the audio chip:

1. **The Pull-Up Resistor (10kΩ):** Connect one leg directly to your main **+5V (VCC)** power rail, and connect the other leg to **Pin 23 (/RESET)** of the YM2149.
2. **The Capacitor (10µF):** Connect the positive (+) terminal (usually the longer leg) to **Pin 23 (/RESET)** of the YM2149, and connect the negative (-) terminal directly to your common **Ground plane (GND)**.
3. **The Reset Switch (Normally Open):** Wire the two primary contacts in parallel across the capacitor. Connect one pin to **Pin 23 (/RESET)** of the YM2149, and the other pin to **Ground (GND)**.

#### Theory of Operation
1. **Power-On Reset (POR):** At initial power-up, the capacitor is empty, acting as a temporary short to Ground. This safely forces `/RESET` low while the main system +5V rail stabilizes. The capacitor slowly charges through the `10kΩ` pull-up resistor, releasing the reset line to `HIGH` once voltage conditions match normal operating constraints.
2. **Manual Hard Reset:** Pressing the normally-open tactile switch instantly discharges the capacitor directly to Ground, forcing a clean software-independent hardware re-initialization of the audio chip registers.

### 5. LM358 Audio Stage (Active-Passive Hybrid Shunt Mixer)

The audio stage uses an LM358 op-amp in a parallel **Active-Passive Hybrid Shunt** configuration. This design uses the op-amp's feedback loop and single-supply saturation limits to act as a passive load that prevents console audio clipping, while incorporating an AC shunt network to smooth out high-frequency square wave edges.

| Pin | Signal | Connection |
| :--- | :--- | :--- |
| **1** | OUT1 | **Op-amp Output** (Feedback loop to Pin 2) |
| **2** | IN1_NEG | **Summing Node** (Connected directly to audio line) |
| **3** | IN1_POS | **Ground** |
| **4** | GND | **Ground Plane** |
| **5** | IN2_POS | **Ground** (Unused channel stability) |
| **6** | IN2_NEG | **OUT2** (Pin 7) (Unused channel stability) |
| **7** | OUT2 | **IN2_NEG** (Pin 6) (Unused channel stability) |
| **8** | VCC | **+5V** |

> **Note:** The unused second channel (Pins 5–7) is configured as a unity-gain buffer tied to ground to prevent oscillation and thermal instability.

> **Musician's Note on Op-Amps:** While this circuit is pin-compatible with higher-end op-amps like the **TL072**, real-world testing on the Atari 7800 showed that the humble **LM358** actually produced a more desirable "retro" tone. The LM358's performance on the single 5V rail adds a slight warmth and grit that perfectly complements the YM2149 PSG. That said, any pin-compatible op-amp can be used here.

**Audio Path Details:**
*   **Passive Mixing Node**: YM2149 Channels A, B, and C each go through a **1kΩ isolation resistor** to a single **Summing Node**, which is wired directly to the Atari 7800 **Pin 18 (Exaudio)** input.
*   **Feedback Loop**: A **1kΩ resistor** connects the Summing Node (Pin 2) to the op-amp Output (Pin 1). Since Pin 3 is grounded and inputs are positive, the op-amp saturates at `0V`, making this resistor behave as a passive `1kΩ` load to Ground to prevent console clipping.
*   **Class-A Bias Pull-Down**: A **1kΩ resistor** connects from Pin 1 (OUT1) to Ground (Pin 4) to bias the LM358's output stage into Class-A operation, eliminating crossover distortion.
*   **AC Shunt Network**: A **1kΩ resistor** in series with a **10µF capacitor** connects Pin 1 (OUT1 / `0V`) to the Summing Node. At audio frequencies, the capacitor acts as a short, putting the second `1kΩ` resistor in parallel with the feedback resistor to decrease AC load impedance to `500Ω`, smoothing square wave transients.


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
| **9** | A1 (7800 Cart Pin 25) | **20** | !CE (Chip Enable - Logic Pin 19) |
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
| **15** | IOA6 | **26** | N.C. |
| **16** | IOA5 | **25** | A8 (Address 8 - Tie High) |
| **17** | IOA4 | **24** | !A9 (Address 9 - Tie Low) |
| **18** | IOA3 | **23** | !RESET (Reset - Tie High) |
| **19** | IOA2 | **22** | CLOCK (Master Clock Input) |
| **20** | IOA1 | **21** | IOA0 |

