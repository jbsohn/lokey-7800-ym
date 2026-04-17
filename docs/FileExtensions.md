# Atari 7800 YM2149 Project File Reference

This document describes the various file extensions and build artifacts used in the YM2149 toolchain.

## Asset & Source Files

| Extension | Name | Description |
| :--- | :--- | :--- |
| `.ym` / `.YM` | YM Sample | Original Atari ST / Amstrad CPC music files (YM2149 register dumps). |
| `.vgm` / `.vgz` | VGM Sample | Video Game Music files containing AY-3-8910 command streams. |
| `.asm` | 6502 Assembly | Source code for the music player, logic ROMs, and hardware drivers. |
| `.pld` | CUPL Logic | Source code for GAL16V8 Programmable Logic Devices (address decoding). |

## Intermediate Build Artifacts

| Extension | Name | Description |
| :--- | :--- | :--- |
| **`.ymb`** | **YM Binary** | Custom compressed music data. Uses delta-masking and pattern deduplication. Replaced generic `.bin`. |
| **`.ymi`** | **YM Info** | Sidecar metadata file for DASM. Contains timing constants (`PLAYER_HZ`, `YM_DELAY`) and data offsets. Replaced `.yminc`. |
| `.wav` | Verification Audio | 44.1kHz PCM audio rendered from the `.ymb` data to verify conversion accuracy. |

## Target ROMs & Hardware Files

| Extension | Name | Description |
| :--- | :--- | :--- |
| **`.rom`** | **Hardware ROM** | Raw 32KB binary image. These are **signed** via `7800sign` and are ready to be flashed to physical EPROMs. |
| **`.a78`** | **Emulator ROM** | Includes a standard 128-byte Atari 7800 header. Used for testing in emulators like A7800, B7800, or JS7800. |
| `.jed` | JEDEC Logic | Compiled fuse map for the GAL16V8, generated from `.pld` files via `galette`. |
