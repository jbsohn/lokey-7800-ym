# Atari 7800 YM2149 Project Agents

This document defines specialized subagents for the Lokey 7800 YM project. These agents provide deep expertise in specific domains of the project, from 6502 assembly to PCB design.

---

## 6502 Assembly Expert
**Expertise:** DASM Assembly, Atari 7800 Hardware, YM2149 Player Logic.

### Instructions
- Follow the DASM coding style defined in `CLAUDE.md`.
- Ensure all ROMs start at `$8000` and have correct vectors at `$FFFA-$FFFF`.
- Use `ay_addr = $0800` and `ay_data = $0801` for YM2149 communication.
- Optimize for cycle counting in VBI (Vertical Blank Interrupt) routines.
- Refer to `examples/` for implementation patterns of the YM2149 player.

---

## tscircuit PCB Designer
**Expertise:** React-based PCB Design, `tscircuit` CLI, Hardware Schematics.

### Instructions
- Use the `tscircuit` React components in `pcb/`.
- **Source of Truth**: Always ensure the PCB design matches the specifications in `docs/Hardware.md`. This is the authoritative reference for pinouts and logic.
- Ensure all components (74HCT373, YM2149, 27C256) are correctly decoupled.
- Follow the pinout for the Atari 7800 Edge Connector as defined in `pcb/Atari7800EdgeConnector.tsx` (which must match `docs/Hardware.md`).
- Keep the board size within standard cartridge dimensions.
- Use the skills in `pcb/.claude/skills/tscircuit/` for best practices.

---

## .NET Tooling Expert
**Expertise:** C# / .NET 8.0+, CLI Tool Development, Binary File Processing.

### Instructions
- Maintain and extend the conversion tools in `tools-cs/`.
- Ensure `VgmToYmb`, `YmToYmb`, and `YmbToWav` remain compatible with the `.ymb` format.
- Use `Core.csproj` for shared logic between tools.
- Optimize for large music files and ensure memory efficiency during conversion.
- Follow the existing command-line argument patterns in `CommandLineUtils.cs`.

---

## Music/Sound Specialist
**Expertise:** YM2149 Register Architecture, VGM/YM File Formats, .YMB Binary Format.

### Instructions
- Understand the 14 registers of the YM2149 (AY-3-8910 compatible).
- Manage the relationship between original clock frequencies and the 7800's implementation.
- Optimize music data for the `.ymb` format to fit within 32KB ROM constraints.
- Verify music playback using `YmbToWav` and comparing against original sources in `ym-samples/`.
- Refer to `docs/YmbFormat.md` for binary structure details.
