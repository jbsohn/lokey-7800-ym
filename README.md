# 7800 YM2149 Lab

![Atari 7800 YM2149 Hardware Lab](docs/7800-ym-lab.jpg)

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

## Hardware Wiring

To connect a **YM2149** (or **AY-3-8910**) to the Atari 7800 using the provided GAL logic:

### 1. GAL16V8 Pinout ([ym2149_wincupl.pld](file:///home/john/Projects/7800-ym2149-lab/gal/ym2149_wincupl.pld))
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
| 30-37 | DA7-DA0 | 7800 Data Bus (D7-D0) |
| 40 | VCC | +5V |

3.  **Clocking**: Pin 22 (CLOCK) typically receives PHI2OUT from the GAL (1.79MHz) for 1:1 emulator parity. However, the logic is robust enough to support an external 2MHz crystal directly; this provides exact Atari ST sound compatibility for imported assets without needing software frequency adjustments.

## Baseline ROM: `ym2149_heartbeat_main.asm`

This is our "Gold Standard" baseline. It verified that:
1.  **Pitch**: Matches the emulator perfectly.
2.  **Stability**: No notes are dropped (Uses Quad-Tap writes).
3.  **Cleanliness**: No stuttering (HALT pulses protect the bus).

**Tempo**: 1.0 seconds per note (Ideal for hardware verification).

> **IMPORTANT**
> The GAL logic uses **HALT-Protected Asynchronous** pulses. This expands the write pulse width to ~550ns (meeting the 300ns requirement) while ensuring the Maria graphics chip cannot corrupt registers.

## AI Assistance

This project was developed with significant assistance from AI (Antigravity). For the author, AI has been a "force multiplier"—making it possible to tackle long-held "I've always wanted to do this" projects within the limited hours of evenings and weekends. 

For reflections on how AI is changing the landscape of hardware and software side-projects, see the author's blog post:

[What AI Is Doing to Software Development (and What It Can’t Do Yet)](https://johnsmusicandtech.com/posts/what-ai-is-doing-to-software-development/)
