# Toolchain & Diagnostics Guide

This document describes the compilation, conversion, verification, and diagnostic tools used in the Lokey-YM project. All tools require the **.NET SDK**.

> [!IMPORTANT]
> These C# tools are **experimental** and primarily intended for **early testing** of the YM2149 on Atari 7800 cartridges.
> 
> The C# implementation was a temporary hack to quickly parse and verify Atari ST captures. In future development, these tools will be rewritten in C++ and optimized for a driver architecture that supports simultaneous sound effects and music playback.

> [!NOTE]
> The current toolchain and the `.ymb` music format were developed as a proof of concept to quickly get YM music playing on physical cartridges, and are subject to change in future revisions.

---

## 1. Music Conversion & Verification

We provide tools to convert register streams into our custom, compressed `.ymb` music format and verify the output.

### `YmToYmb` (Atari ST Captures)
Converts Atari ST **YM** files (register dumps from legacy ST trackers).
*   **Formats**: YM2, YM3, YM4, YM5, and YM6.
*   **Usage**: 
    ```bash
    dotnet run --project tools-cs/YmToYmb/YmToYmb.csproj -- <input.ym> [options]
    ```

### `VgmToYmb` (Modern Trackers)
Converts **VGM/VGZ** command streams. This is the preferred route for new compositions.
*   **Trackers**: [Furnace Tracker](https://tildearrow.org/furnace) or [Arkos Tracker](https://www.julien-nevo.com/arkostracker/).
*   **Usage**: 
    ```bash
    dotnet run --project tools-cs/VgmToYmb/VgmToYmb.csproj -- <input.vgm> [options]
    ```

### `YmbToWav` (Audio Verification)
Renders `.ymb` music data back into standard `.wav` audio to verify conversion accuracy.
*   **Accuracy**: Aims to reproduce the YM2149's logarithmic volume curve and hardware envelopes.
*   **Usage**: 
    ```bash
    dotnet run --project tools-cs/YmbToWav/YmbToWav.csproj -- build/song.ymb [output.wav]
    ```

---

## 2. Core Music Processing Theory

For the complete technical layout of the generated output, see the **[YMB Format Technical Specification](YmbFormat.md)**.

1.  **Pitch Scaling (The Clock Problem)**: The Atari ST's YM2149 chip is clocked at **2.0 MHz**, while the Atari 7800's PHI2 clock (which drives the YM chip in this project) is **~1.79 MHz** (NTSC). Without scaling, notes would sound flat. The tools multiply frequency and noise registers by a `pitchScale` ratio (~0.895) so music plays in tune.
2.  **Delta Masking**: To avoid writing 14 bytes/frame, a **16-bit bitmask** precedes every frame. If a bit is `1`, the register value follows. If `0`, the register remains unchanged from the previous frame. Silent or repetitive frames shrink from 14 bytes down to just 2 bytes.
3.  **Pattern Deduplication**: The converter slices songs into fixed-size patterns, identifies identical patterns, and stores only unique pattern blocks. It automatically tests multiple pattern sizes to find the best compression ratio.

---

## 3. Hardware Signal Diagnostics

These scripts help validate physical signals coming off the Atari 7800 cartridge edge connector before they reach the ROM or YM2149.

### `tools-cs/Scripts/ValidateCartSignals.cs`
Validates physical signals coming off the edge connector.
*   **Requirements**: `sigrok-cli` and a logic analyzer (e.g., `fx2lafw`).
*   **Target Signals**: Analyzes `PHI2`, `R/W`, `HALT`, and `A15` for stable transitions.

### `tools-cs/Scripts/ValidateLogicSignals.cs` & `tools-cs/Scripts/ValidateLatchEnable.cs`
Advanced timing diagnostics used to detect mid-cycle address bus noise or "shattered" pulses.
*   **Timing Check**: Ensures `YM_LE`, `BDIR`, and `BC1` only trigger when the `PHI2` clock is stable.
*   **BC1 Monitor**: Confirms the logic equations correctly identify register selection (A0=0) vs. data writes.

---

## 4. ROM Packaging Tools

### `tools-cs/A78Gen/Program.cs`
Generates a 128-byte A78 header and packages a raw binary for emulator use.
*   **Status**: **Work in Progress**.
*   **Mapper**: set `"mapper": 1` in the config JSON to package the 32-pin board's YM-IOA bank scheme (see [Emulation.md](Emulation.md#a78-header--emulator-detection)) — the input binary must then be the full 128KB or 256KB ROM image, not just the fixed 32KB bank. `"mapper": 0` (default) keeps the original fixed-32KB-only behavior.
*   **Usage**:
    ```bash
    dotnet run --project tools-cs/A78Gen/A78Gen.csproj -- <input.bin> <config.json> -o <output.a78>
    ```
*   **Planned Improvements**: Mapping for other cartridge header types (SuperGame, Activision, etc.) beyond the fixed-32KB and YM-IOA-banked cases.
