# Emulator Support

To iterate rapidly without burning EPROMs, you can use these specialized forks that include full support for the physical YM2149 hardware mapping.

## A78 Header & Emulator Detection

The project uses a **v4 A78 Header** (an extension of the standard 128-byte header) to signal to the emulator that YM2149 hardware is present.

- **Header Version**: `4` (Offset 0)
- **Audio Flags (Hi)**: Bit 6 is set (`%01000000`) to indicate YM2149 presence (Offset 66).
- **Mapper**: `0` (Linear/No Bankswitching, Offset 64).

When the `a7800` or `js7800` forks detect this flag in a `.a78` file, they automatically enable the YM2149 engine and map it to the **$4000–$7FFF** range.

## a7800 (Desktop)
A desktop emulator for the 7800, updated here to include support for this YM2149 memory mapping.

- **Repository**: [https://github.com/jbsohn/a7800](https://github.com/jbsohn/a7800)
- **Branch**: `ym2149`
- **Key Enhancements**:
  - **Native Apple Silicon Support**: Runs natively on **macOS M1/M2/M3** CPUs.
  - **Hardware Accuracy**: Implements the exact 16000-byte physical memory mapping ($4000–$7FFF) used by this project.
  - **AY/YM Engine**: Full emulation of the YM2149 PSG, synchronized with the 7800 PHI2 clock.

## js7800 (Web-based)

A browser-based emulator that allows for zero-setup testing and sharing.

- **Live Demo**: [**Play the YM2149 Demo in your Browser**](https://jbsohn.github.io/js7800-ym-player/)
- **Repository**: [https://github.com/jbsohn/js7800](https://github.com/jbsohn/js7800)
- **Branch**: `ym2149`
- **Key Enhancements**:
  - **WebAudio Integration**: Bridges the 6502 register writes to the browser's audio engine for real-time playback.
  - **Rapid Iteration**: Open sourceyour `.a78` builds directly into the browser.
