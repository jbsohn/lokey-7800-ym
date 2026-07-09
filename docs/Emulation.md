# Emulator Support

To iterate rapidly without burning EPROMs, you can use these specialized forks that include full support for the physical YM2149 hardware mapping.

## A78 Header & Emulator Detection

The project uses a **v4 A78 Header** (an extension of the standard 128-byte header) to signal to the emulator that YM2149 hardware is present.

- **Header Version**: `4` (Offset 0)
- **Audio Location**: `$0800` written into Offset 66–67 (`A78Gen`'s `audio` field) — the real YM2149 address register location.
- **Cart Type Flag**: Bit 2 of the Cart Type low byte (Offset 54) is force-set as a redundant "YM2149 present" flag for older emulator support.
- **Mapper**: `0` (Linear/No Bankswitching, Offset 64).

> **Emulator fork compatibility note**: As of the move to the $0800/$0801 "Pokey800" mapping (see [Hardware.md](Hardware.md#memory-mapping)), Offset 66 is `$08` rather than `$40`. The `a7800`/`js7800` forks below historically detected YM2149 presence by checking **bit 6** of Offset 66 (`%01000000`), which only happened to work because $4000's high byte is `0x40`. That bit-6 check no longer matches and needs a corresponding update in those forks — until then, rely on the Cart Type bit 2 flag (Offset 54) for detection, or patch the forks to read the full 16-bit Offset 66–67 value as the actual mapped address instead of a fixed bit flag.

When the `a7800` or `js7800` forks detect YM2149 hardware in a `.a78` file, they automatically enable the YM2149 engine and map it to the **$0800–$0801** range.

## a7800 (Desktop)
A desktop emulator for the 7800, updated here to include support for this YM2149 memory mapping.

- **Repository**: [https://github.com/jbsohn/a7800](https://github.com/jbsohn/a7800)
- **Branch**: `ym2149`
- **Key Enhancements**:
  - **Native Apple Silicon Support**: Runs natively on **macOS M1/M2/M3** CPUs.
  - **Hardware Accuracy**: Implements the physical memory mapping ($0800–$0801) used by this project. *(Needs update for the Pokey800 remap — see compatibility note above.)*
  - **AY/YM Engine**: Full emulation of the YM2149 PSG, synchronized with the 7800 PHI2 clock.

## js7800 (Web-based)

A browser-based emulator that allows for zero-setup testing and sharing.

- **Live Demo**: [**Play the YM2149 Demo in your Browser**](https://jbsohn.github.io/js7800-ym-player/)
- **Repository**: [https://github.com/jbsohn/js7800](https://github.com/jbsohn/js7800)
- **Branch**: `ym2149`
- **Key Enhancements**:
  - **WebAudio Integration**: Bridges the 6502 register writes to the browser's audio engine for real-time playback.
  - **Rapid Iteration**: Open sourceyour `.a78` builds directly into the browser.
