# YM to Atari 7800 Binary Converter (`YmToBin.cs`)

This tool converts Atari ST YM music files into a highly compressed, pitch-corrected binary format specifically optimized for the Atari 7800's 6502 CPU and YM2149 sound chip.

## Core Processing Theory

### 1. Pitch Scaling (The Clock Problem)
The Atari ST's YM2149 chip is clocked at **2.0 MHz**, while the Atari 7800's PHI2 clock (which drives the YM chip in this project) is **~1.79 MHz** (NTSC).
*   **Without Scaling:** Every note would sound roughly **2 semitones flat**.
*   **The Solution:** The tool calculates a `pitchScale` ratio ($1.79 / 2.0 = 0.895$). Every frequency value (registers 0–5) and noise value (register 6) is multiplied by this ratio to ensure the music stays in tune on the 7800.

### 2. Delta Masking (Bandwidth Optimization)
Instead of storing all 14 YM registers for every frame (14 bytes/frame), the tool uses **Delta Masking**:
*   A **16-bit bitmask** precedes every frame.
*   Each bit represents one of the 14 registers.
*   If a bit is `1`, the register value follows the mask.
*   If a bit is `0`, the register hasn't changed since the previous frame, and the player routine should skip it.
*   **Result:** Silent or repetitive frames take only 2 bytes (the mask) instead of 14.

### 3. Pattern Deduplication (O(N) Optimization)
To fit long songs into small ROMs, the song is sliced into fixed-size **Patterns** (e.g., 64 frames each).
*   **Deduplication:** The tool identifies identical patterns (like a repeating bassline) and stores the data only once.
*   **O(N) Lookup:** It uses a `Dictionary<byte[], int>` with a custom `ByteArrayComparer` to find duplicates in a single pass ($O(N)$), rather than re-searching the whole song ($O(N^2)$).

---

## C# 12 Implementation Details

The script uses modern C# features to achieve high performance with a clean codebase.

### Memory Management with `Span<T>`
Instead of allocating thousands of small arrays during processing, the tool uses **Spans**:
*   `ReadOnlySpan<byte>`: Allows slicing the input file (`data[12..16]`) without copying memory.
*   `stackalloc byte[NumRegisters]`: Allocates temporary frame buffers on the **Stack** instead of the **Heap**, resulting in zero Garbage Collection (GC) overhead.

### Data Modeling
*   **`internal record YmHeader`**: A reference-type record for file metadata. It provides built-in immutability and the `with` keyword for easy copying.
*   **`internal record struct ConversionOptions`**: A value-type record struct for command-line arguments. It is passed by value (fast for small data) and lives on the stack.

### Modern Syntax
*   **Collection Expressions**: Uses `[...]` instead of `new[] { ... }` for a cleaner, unified way to create arrays and lists.
*   **Raw String Literals**: Uses `"""` for the assembly include template, allowing multi-line strings with preserved indentation and no need for escape characters.
*   **Pattern Matching**: Uses `sig is "YM2!" or "YM3!"` for highly readable logical checks.

---

## Binary Format Specification

The generated `.bin` file is structured for fast reading by a 6502 assembly routine:

| Size | Description |
| :--- | :--- |
| 1 byte | **Pattern Size** (number of frames per block) |
| 1 byte | **Unique Pattern Count** |
| 1 byte | **Sequence Length** |
| N bytes | **Sequence Table**: A list of unique pattern IDs to play in order. |
| M*2 bytes| **Offset Table**: 16-bit relative pointers to the start of each unique pattern. |
| Variable | **Pattern Data**: The delta-masked frames: `[2-byte Mask][Changed Bytes...]` |

## Assembly Configuration (`.yminc`)
The tool outputs a `.yminc` file containing constants for your 6502 player:
*   `YM_DELAY` & `YM_FINE`: Constants for a delay loop to maintain correct playback speed (50Hz/60Hz) despite the 6502's clock speed.
*   `MAX_FRAMES`, `SEQ_LEN`, etc.: Metadata to help the assembly code navigate the binary data.
