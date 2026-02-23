# 7800 YM2149 Lab

This repository is a playground for experiments with the Atari 7800 using YM2149 on cartridge.

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
/home/john/7800AsmDevKit/7800sign -w ym2149_melody.bin
/home/john/7800AsmDevKit/7800sign -t ym2149_melody.bin
/home/john/7800AsmDevKit/7800sign -w ym2149_simple_tone.bin
/home/john/7800AsmDevKit/7800sign -t ym2149_simple_tone.bin
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

For real hardware, start with `ym2149_simple_tone.bin`, then move to the melody/lullaby ROMs.
