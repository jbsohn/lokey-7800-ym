# Music Conversion & Verification Tools

This document describes the tools used to process and verify music for the Atari 7800 YM2149 bridge. All tools require the **.NET SDK**.

> [!IMPORTANT]
> These C# tools are **experimental** and primarily intended for **early testing** of the YM on Atari 7800 cartridges. A core goal is to minimize data size for limited ROM space.

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

### `YmsCompile` (Procedural Sound Design)
The preferred way to create sound effects. Compiles a human-readable **JSON Recipe** into a surgical 14-register bitstream.
- **Workflow**: Create a `.json` recipe, compile to `.yms`, and verify with `YmsToWav`.
- **Usage**: `dotnet run --project tools/YmsCompile/YmsCompile.csproj -- <recipe.json> [options]`

---

## Procedural Sound Recipes (.json)

A recipe allows you to define a sound mathematically. Example:

```json
{
  "name": "Laser",
  "track": "ChannelA",
  "commands": [
    { "type": "set", "pitch": 50, "volume": 15 },
    { "type": "slide", "target_pitch": 400, "duration": 10 },
    { "type": "fade", "target_volume": 0, "duration": 5 }
  ]
}
```

---

## Core Processing Theory

### 1. Unified Stream Architecture
To keep the 6502 replayer as simple as possible, both music and sound effects use the same **14-register interleaved bitmask** format. 
*   **Music**: Uses a pattern/sequence container for ROM efficiency.
*   **SFX**: A flat, non-repeating stream for surgical events.

### 2. Pitch Scaling (The Clock Problem)
The Atari ST's YM2149 chip is clocked at **2.0 MHz**, while the Atari 7800's PHI2 clock (which drives the YM chip in this project) is **~1.79 MHz** (NTSC).
*   **The Solution:** The tools calculate a `pitchScale` ratio ($1.79 / 2.0 = 0.895$). Frequency and noise registers are multiplied by this ratio to help the music stay in tune on the 7800.

### 3. Delta Masking
Every frame is preceded by a **16-bit bitmask**:
*   Each bit represents one of the 14 registers (0-13).
*   If a bit is `1`, the register value follows the mask.
*   If a bit is `0`, the register hasn't changed.
*   **Result**: Silent or repetitive frames take only 2 bytes (the mask) instead of 14.

### 4. Pattern Deduplication (Music Only)
Music is sliced into fixed-size **Patterns** and compressed.
*   **Deduplication**: Identical patterns are stored once.
*   **Optimization**: The tool automatically tests multiple pattern sizes (16 to 255) to find the best compression ratio.

---

## Binary Format Specification (.ymb)

| Size | Description |
| :--- | :--- |
| 1 byte | **Pattern Size** (frames per block) |
| 1 byte | **Unique Pattern Count** |
| 1 byte | **Sequence Length** |
| N bytes | **Sequence Table**: List of unique pattern IDs. |
| M*2 bytes| **Offset Table**: 16-bit relative pointers to pattern data. |
| Variable | **Pattern Data**: Delta-masked updates: `[2-byte Mask][Changed Bytes...]` |

---

## Audio Verification

### `YmbToWav`
A verification tool that renders `.ymb` music data back into standard `.wav` audio. 

- **Accuracy**: Aims to reproduce the YM2149's logarithmic volume curve and hardware envelopes.
- **Filtering**: Uses downsampling to reduce digital aliasing.
- **Usage**: `dotnet run --project tools/YmbToWav/YmbToWav.csproj -- build/song.ymb [output.wav]`
