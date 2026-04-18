# Music Conversion & Verification Tools

This document describes the tools used to process and verify music for the Atari 7800 YM2149 bridge. All tools require the **.NET SDK**.

## Music Conversion

We provide two primary tools for converting register streams into the optimized `.ymb` format.

### `YmToYmb` (Atari ST Captures)
Converts Atari ST **YM** files (captured from legacy ST trackers). 
- **Formats**: YM2, YM3, YM4, YM5, and YM6.
- **Usage**: `dotnet run --project tools/YmToYmb/YmToYmb.csproj -- <input.ym> [options]`

### `VgmToYmb` (Modern Trackers)
Converts **VGM/VGZ** command streams. This is the preferred route for new compositions.
- **Trackers**: [**Furnace Tracker**](https://tildearrow.org/furnace) or [**Arkos Tracker**](https://www.julien-nevo.com/arkostracker/) (dedicated specifically to the AY/YM architecture).
- **Usage**: `dotnet run --project tools/VgmToYmb/VgmToYmb.csproj -- <input.vgm> [options]`

---

## Core Processing Theory

### 1. Pitch Scaling (The Clock Problem)
The Atari ST's YM2149 chip is clocked at **2.0 MHz**, while the Atari 7800's PHI2 clock (which drives the YM chip in this project) is **~1.79 MHz** (NTSC).
*   **Without Scaling:** Notes would sound roughly **2 semitones flat**.
*   **The Solution:** The tools calculate a `pitchScale` ratio ($1.79 / 2.0 = 0.895$). Frequency and noise registers are multiplied by this ratio to help the music stay in tune on the 7800.

### 2. Delta Masking
Instead of storing all 14 YM registers for every frame (14 bytes/frame), we use **Delta Masking**:
*   A **16-bit bitmask** precedes every frame.
*   Each bit represents one of the 14 registers.
*   If a bit is `1`, the register value follows the mask.
*   If a bit is `0`, the register hasn't changed since the previous frame.
*   **Result**: Silent or repetitive frames take only 2 bytes (the mask) instead of 14.

### 3. Pattern Deduplication
To fit songs into a 32KB ROM, the data is sliced into fixed-size **Patterns**.
*   **Deduplication**: The tool identifies identical patterns and stores the data once.
*   **Optimization**: The tool automatically tests multiple pattern sizes to find the best compression ratio.

---

## Binary Format Specification (.ymb)

The generated `.ymb` file is structured for fast reading by a 6502 assembly routine. For more details, see the **[YMB Format Technical Specification](YmbFormat.md)**.

| Size | Description |
| :--- | :--- |
| 1 byte | **Pattern Size** (number of frames per block, Max 255) |
| 1 byte | **Unique Pattern Count** (Max 255) |
| 1 byte | **Sequence Length** (Max 255) |
| N bytes | **Sequence Table**: A list of unique pattern IDs to play in order. |
| M*2 bytes| **Offset Table**: 16-bit relative pointers to the start of each unique pattern. |
| Variable | **Pattern Data**: The delta-masked frames: `[2-byte Mask][Changed Bytes...]` |

## Assembly Configuration (.ymi)
The tool outputs a `.ymi` file containing constants for the 6502 player:
*   `YM_DELAY` & `YM_FINE`: Constants for a delay loop to maintain correct playback speed.
*   `MAX_FRAMES`, `SEQ_LEN`, etc.: Metadata to help the assembly code navigate the binary data.

---

## Audio Verification

### `YmbToWav`
A verification tool that renders `.ymb` music data back into standard `.wav` audio. 

- **Accuracy**: Aims to reproduce the YM2149's logarithmic volume curve and hardware envelopes.
- **Filtering**: Uses downsampling to reduce digital aliasing.
- **Usage**: `dotnet run --project tools/YmbToWav/YmbToWav.csproj -- build/song.ymb [output.wav]`
