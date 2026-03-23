# 7800 YM2149 Lab

## **WARNING**

> **WORK IN PROGRESS / ALPHA STATE**\
> This laboratory is an active research project. The hardware has unstable feedback; it is sort of working but needs to be perfected. The codebase, wiring diagrams, and tools are subject to frequent breaking changes as we optimize for a "Gold Standard" release. Proceed with curiosity and caution!

![Atari 7800 YM2149 Hardware Lab](docs/7800-ym-lab.jpg)

### Hardware Tests in Action
Check out the current state of tests running on the dev board:

[![Hardware Test 1](https://img.youtube.com/vi/xwr_qn-GMdQ/hqdefault.jpg)](https://www.youtube.com/shorts/xwr_qn-GMdQ)


[![Hardware Test 2](https://img.youtube.com/vi/qCsVi0Iiq5I/hqdefault.jpg)](https://www.youtube.com/shorts/qCsVi0Iiq5I)

This repository is a playground for experiments with the Atari 7800 using YM2149 on cartridge.

## Why this project?

The goal is to provide a stable, low-cost ($~2 USD) bridge between the Atari 7800 and the Atari ST. By leveraging the YM2149 PSG—or modern clones like the **KC89C72** (used in this project's lab) which are still in production—we can bring rich, standardized three-channel sound to the 7800 with 100% Atari ST asset compatibility. Original YM2149s remain widely available as used or New-Old-Stock (NOS) at similar price points.

## Build

By default, sources build with the 128-byte A78 header (good for emulators):

```bash
make a78
```

To build raw ROM images (no A78 header, good for EPROM burning), set
`build_with_header=0`:

```bash
make bin
```

## Emulator Support

Before testing on real hardware, you can verify your builds using a specialized branch of the **a7800** emulator that supports this physical YM2149 mapping:

- **Repository**: [https://github.com/jbsohn/a7800](https://github.com/jbsohn/a7800)
- **Branch**: `ym2149`

### Compatibility Notes:
- **Modern Hardware**: This branch includes specific updates for **macOS on Apple Silicon (M1/M2/M3)**.
- **C# / .NET Tooling**: All diagnostic and processing tools require the **.NET SDK** (verified on Linux and macOS).
- **Supported Platforms**: Built and tested for **macOS** and **Linux**.
- **Windows**: Currently **untested**. If you are on Windows, your mileage may vary as the build environment has not been verified for that platform yet.

This is the recommended way to iterate on your code and musical assets before committing to a hardware burn.

## Signing for Real 7800 Hardware

Atari 7800 cartridges must be cryptographically signed. After building a raw
`.bin`, run `7800sign -w` to write the signature into the ROM image:

```bash
7800sign -w ym2149_heartbeat_main.bin
7800sign -t ym2149_heartbeat_main.bin
7800sign -w ym2149_melody_vbi.bin
7800sign -t ym2149_melody_vbi.bin
```

Important ROM footer requirement for `7800sign`:

- `$FFF8` must be `$FF`
- `$FFF9` low nibble must be `3` or `7` (for a 32KB image at `$8000`, use `$83`)

In assembly source:

```asm
org $fff8
.byte $ff
.byte $83
org $fffa
.word reset
.word reset
.word reset
```

## Output Formats

- `.a78`: 128-byte A78 header + 32 KB ROM (`32896` bytes total). Use for emulators.
- `.bin`: raw 32 KB ROM only (`32768` bytes). Use for EPROM programming / real cartridge testing.

For real hardware, start with **ym2149_heartbeat_main.bin**. It is our "Gold Standard" baseline that has been verified 100% stable on the Atari 7800.

## Memory Mapping & POKEY Compatibility

The YM2149 sound card in this lab is mapped to the **$4000–$7FFF** range (16 KB). 
### Write-Only Mirroring (Theoretical)

The current GAL logic is gated by the `!RW` (Read/Write) line. This means the YM2149 should effectively be a "write-only" device at $4000. In theory, this allows other devices (like ROM or RAM) to reside at the same memory addresses for **read** operations without bus contention. 

> **NOTE:** This "Stealth Mirroring" is the intended design but remains untested on live hardware. It represents one of our "hopes" for maximum bus efficiency!

This mapping is intentional: it follows the historical precedent set by classic Atari 7800 games like **Ballblazer** and **Commando**, which mapped the **POKEY** sound chip to $4000. By mirroring this 16k "Sound Area," we ensure high compatibility with existing hardware designs and make it easier for 7800 developers to swap or supplement POKEY with the YM2149.

## GAL Logic

This project provides two `GAL16V8` programming files for address decoding:
1. **[rom.pld](gal/rom.pld)**: A simple 32KB ROM decoder. It removes the need for basic LS04/LS02 logic chips and is intended for initial hardware testing before adding the sound chip.
2. **[rom_ym.pld](gal/rom_ym.pld)**: The full decoder. It includes the 32KB ROM decoding plus the logic required to map the YM2149 sound chip to the $4000-$7FFF address space, along with its clock and bus controls.

## Hardware Wiring

To connect a **YM2149** (or **AY-3-8910**) to the Atari 7800 using the provided `rom_ym.pld` logic:

### 1. GAL16V8 Pinout (`rom_ym.pld`)
| Pin | Signal | Source |
| :--- | :--- | :--- |
| 2 | A15 | 7800 Address Bus |
| 3 | A14 | 7800 Address Bus |
| 4 | A0 | 7800 Address Bus |
| 5 | HALT | 7800 Maria Halt Signal |
| 6 | R/W | 7800 CPU R/W Line |
| 7 | PHI2 | 7800 CPU Clock (Pin 28 on Cart) |
| 16 | **PHI2OUT** | Buffered Clock to YM Pin 22 |
| 17 | **BC1** | Connect to YM Pin 29 |
| 18 | **BDIR** | Connect to YM Pin 27 |
| 20 | VCC | +5V |

### 2. YM2149 / AY-3-8910 Connections
| YM Pin | Signal | Connection |
| :--- | :--- | :--- |
| 1 | GND | Ground |
| 22 | CLOCK | **PHI2OUT (GAL Pin 16)** |
| 23 | /RESET | +5V |
| 24 | /A9 | GND |
| 25 | A8 | +5V |
| 27 | BDIR | GAL Pin 18 |
| 28 | BC2 | +5V |
| 29 | BC1 | GAL Pin 17 |
| 30 | DA7 | 7800 Data Bus D7 |
| 31 | DA6 | 7800 Data Bus D6 |
| 32 | DA5 | 7800 Data Bus D5 |
| 33 | DA4 | 7800 Data Bus D4 |
| 34 | DA3 | 7800 Data Bus D3 |
| 35 | DA2 | 7800 Data Bus D2 |
| 36 | DA1 | 7800 Data Bus D1 |
| 37 | DA0 | 7800 Data Bus D0 |
| 40 | VCC | +5V |

3.  **Clocking**: Pin 22 (CLOCK) typically receives PHI2OUT from the GAL (1.79MHz) for 1:1 emulator parity. However, the logic is robust enough to support an external 2MHz crystal directly; this provides exact Atari ST sound compatibility for imported assets without needing software frequency adjustments.

## Included Tools

### `tools/ValidateCartSignals.cs`
A diagnostic C# script used to validate the raw physical signals coming off the Atari 7800 cartridge edge connector before they reach the ROM or YM2149. 

- **Requirements**: `sigrok-cli` and `dotnet-script` installed globally.
- **Usage**: Run `./tools/ValidateCartSignals.cs` from the project root. It captures 100,000 samples at 24MHz using a logic analyzer (defaults to `fx2lafw`).
- **Output**: Analyzes the CSV stream in real-time and reports the total number of low-to-high transitions for the `PHI2` Clock, `R/W`, `HALT`, and `A15` pins. This proves your physical solder joints and edge-connector logic are completely clean.

## Baseline ROM: `ym2149_heartbeat_main.asm`

This is our "Gold Standard" baseline. It verified that:
1.  **Pitch**: Matches the emulator.
2.  **Stability**: No notes are dropped (Uses Quad-Tap writes).
3.  **Cleanliness**: No stuttering (HALT pulses protect the bus).

**Tempo**: 1.0 seconds per note (Ideal for hardware verification).

## Credits & Samples

The `samples/` directory contains a curated selection of melodic assets sourced from the excellent **StSound** project by **Arnaud Carré (Leonard/OXG)**. These are provided for your own experimentation.

- **Source**: [https://github.com/arnaud-carre/StSound](https://github.com/arnaud-carre/StSound)

We are grateful to the Atari ST community for maintaining such high-quality musical assets. It's heartening to see that there are still plenty of dedicated Atari ST fans out there keeping the 16-bit flame alive!

## Future Plans & Extensibility

- **YM2 as Canonical Source**: We have established **YM2** as the preferred "raw" source format for incoming Atari ST data. Its lack of metadata overhead and predictable register-interleaving makes it the perfect baseline for future compression research.
- **Advanced Compression**: Future research will explore **RLE (Run-Length Encoding)** and custom **Bitpacking** (similar to VGM or YM-Pro formats) to squeeze minutes of high-fidelity music into standard 32KB/48KB 7800 banksets.
- **I/O Port Logging**: The YM2149 I/O ports provide 16 additional lines of communication. We plan to implement a high-speed "Diagnostic Logging" system to stream real-time debug data back from the 7800 to a host machine.
- **Enhanced Interfacing**: Using the spare ports for external controllers or status LEDs to assist in hardware bring-up.

## AI Assistance

This project was developed with significant assistance from AI (Antigravity). For the author, AI has been a "force multiplier"—making it possible to tackle long-held "I've always wanted to do this" projects within the limited hours of evenings and weekends. 

For reflections on the legacy of the Atari ST and how AI is changing the landscape of hardware and software side-projects, see the author's blog posts:

[John's Music & Tech](https://johnsmusicandtech.com/)

## License & No Warranty

This project is licensed under the **GNU General Public License v2.0 (GPL-2.0)**. See the `LICENSE` file for details.

**Use at your own risk.** The author is not responsible for any damage to your hardware, loss of data, or any other issues that may arise from using this code, following the wiring diagrams, or running the provided tools. There is no warranty, Expressed or Implied.
