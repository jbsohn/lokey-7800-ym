# Development Guide

This document outlines the technical standards, build process, and coding style for the Lokey 7800 YM project. It is intended for both human contributors and AI assistants.

## Build Commands

| Command | Purpose |
|---------|---------|
| `make tools` | Build the .NET conversion tools solution |
| `make a78` | Build sample ROMs with 128-byte headers (emulators) |
| `make rom` | Build and sign raw ROM binaries for hardware (.rom) |
| `make bin` | Build music data fragments only (.ymb) |
| `make wav` | Generate WAV verification files from .ymb |
| `make gal` | Build JEDEC files from `gal/*.pld` sources |
| `make clean` | Remove `build/` and generated artifacts |

## Style & Coding Guidelines

### 6502 Assembly (DASM)
- **Architecture**: 32KB Linear ROM (starting at `$8000`).
- **Indentation**: Standard DASM format (Label in col 1, Opcode in col 10+).
- **Naming**: Use `snake_case` for labels and subroutines. 
- **Registers**: Prefix YM2149 registers with `ay_` (e.g., `ay_addr = $4000`).
- **Footer**: Vectors MUST be at `$FFFA-$FFFF`. Signature bytes at `$FFF8-$FFF9`.

```asm
    org $fff8
    .byte $ff, $83    ; $83 for 32KB at $8000
    org $fffa
    .word reset      ; NMI
    .word reset      ; RESET
    .word reset      ; IRQ
```

### File Extensions
- **.rom**: Signed Hardware ROM image.
- **.a78**: Emulator ROM image (with 128-byte header).
- **.ymb**: Compressed Music Binary data.
- **.ymi**: YM Information/Metadata sidecar.

### PCB Design
- **Tool**: [tscircuit](https://tscircuit.com)
- **Source**: React-based circuit code in `pcb/index.circuit.tsx`.

## Music Conversion

Use the provided .NET tools to convert register captures:

```bash
dotnet run --project tools/YmToYmb/YmToYmb.csproj -- <input.ym> -o <output.ymb> -s <step>
```
- `-s 1`: Full fidelity.
- `-s 2`: Half-size (recommended for most tracks).

## Hardware Memory Map

| Address | Device |
|---------|--------|
| `$4000` | YM2149 Address Latch (write-only) |
| `$4001` | YM2149 Data Write (write-only) |
| `$8000-$FFFF` | 32KB ROM |

## Diagnostics

- **Software Heartbeat**: Background color flashing confirms the 6502 is executing and VBI timing is correct.
- **WAV Verification**: `dotnet run --project tools/YmbToWav/YmbToWav.csproj -- build/track.ymb output.wav`

## Project Structure

- `sample-code/`: 6502 assembly sample code and reference player.
- `docs/`: Technical reference and deep-dive guides.
- `tools/`: .NET conversion tools and Sigrok diagnostic scripts.
- `gal/`: Programmable logic (GAL16V8) sources.
- `pcb/`: tscircuit PCB design files.
- `ym-samples/`: Original Atari ST music sources.
- `vgm-samples/`: VGM/VGZ music sources.
