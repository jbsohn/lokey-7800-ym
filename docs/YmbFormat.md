# Atari 7800 YM2149 YMB Music Format

This document specifies the custom binary format used to store compressed YM2149 music assets for the Atari 7800. This format is designed for efficiency in a limited ROM, while aiming for low CPU overhead on the 6502.

## 1. Header Structure

The file begins with a 3-byte fixed header:

| Offset | Size | Name | Description |
| :--- | :--- | :--- | :--- |
| 0 | 1 byte | `PatternSize` | Number of frames in each pattern block (e.g., 32, 64). |
| 1 | 1 byte | `NumUnique` | Total number of unique pattern blocks stored (Max 255). |
| 2 | 1 byte | `SeqLen` | Total number of entries in the sequence table (Max 255). |

## 2. Sequence Table

Following the header is the **Sequence Table**. This table defines the playback order of the song.

* **Size**: `SeqLen` bytes.
* **Format**: Each byte is an index (0 to `NumUnique-1`) pointing to a unique pattern.

## 3. Pattern Offset Table

Immediately following the Sequence Table is the **Offset Table**. This table allows the 6502 player to quickly find the start of each unique pattern in ROM.

* **Size**: `NumUnique * 2` bytes.
* **Format**: 16-bit pointers (Little-Endian).
* **Reference**: These offsets are relative to the start of the **Pattern Data** block.

## 4. Pattern Data

The final and largest block is the raw **Pattern Data**.

* **Compression**: Data is stored using **Bitmask Deltas**.
* **Frame Structure**: Each audio frame consists of:
    1. **Mask (2 bytes)**: A 16-bit little-endian value. Bits 0–13 represent YM2149 registers 0–13.
    2. **Payload (Variable)**: Only the register values corresponding to the `1` bits in the mask are stored, in sequential order.

### Example Frame

If only Register 0 (Tone A Low) and Register 8 (Volume A) changed:
* **Mask**: `0x0101` (Binary `%00000001 00000001`)
* **Data**: `[Reg0 Value][Reg8 Value]`
* **Total Size**: 4 bytes (instead of 14).

## 5. Architectural Advantages

### O(1) Seeking

By using fixed-size patterns and an offset table, the 6502 player can instantly calculate the memory address of any part of the song without parsing linear data.

### Zero Arithmetic

The pitch scaling and timing pre-calculations are performed on the PC at build-time. The 7800 simply dumps the raw bytes into the registers, ensuring that even complex trackers (like Furnace) leave 90%+ of the CPU available for game logic.

### Hardware-Correct Envelopes

Register 13 (Envelope Shape) is treated as "volatile" by the converters. Even if the shape value doesn't change, a write is forced if the tracker composition demands a reset, ensuring the characteristic YM "buzz" is preserved.
