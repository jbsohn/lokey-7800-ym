# Building and Toolchain

This document provides instructions for building the project.

## Build Commands

By default, sources build with the 128-byte A78 header for emulators:

```bash
make a78
```

To build ROM binaries for hardware (signed for EPROM burning):

```bash
make rom
```

To generate verification audio for the music tracks:

```bash
make wav
```

To clean generated artifacts:

```bash
make clean
```

## Tool Requirements

All diagnostic and processing tools in this repository require the **.NET SDK**. They have been verified on **Linux** and **macOS** (Intel and Apple Silicon). 

> **Note on Windows**: These tools are currently **untested** on Windows. While they should run via `dotnet build` or `dotnet run`, the build environment and shell-based diagnostics have not been verified for that platform yet.
```

## Logic Compilation (ATF16V8B)

This project uses [galette](https://github.com/simon-frankau/galette), an open-source logic assembler. This allows for a cross-platform toolchain without requiring legacy Windows tools. Original WinCUPL sources are preserved in `gal/wincupl/` as a reference.

To compile the ATF16V8B logic into JEDEC files:

```bash
make logic
```

## Signing for Real 7800 Hardware
Atari 7800 cartridges must be signed. While `make rom` handles this automatically, you can sign a raw binary manually using `7800sign`:

```bash
7800sign -w build/your_song.rom
7800sign -t build/your_song.rom
```

### ROM Footer Requirements

For the signature to be valid, the ROM must have a specific footer. In assembly:

- `$FFF8` must be `$FF`
- `$FFF9` low nibble must be `3` or `7` (for a 32KB image at `$8000`, use `$83`)

Example DASM source:

```asm
    org $fff8
    .byte $ff
    .byte $83
    org $fffa
    .word reset
    .word reset
    .word reset
```
