# Hardware Diagnostic Tools

This document describes the tools used to validate physical signals and timing for the Atari 7800 YM2149 bridge. All tools require the **.NET SDK**.

> [!IMPORTANT]
> These C# tools are **experimental** and primarily intended for **early testing** of the YM on Atari 7800 cartridges.

For music conversion (`YmToYmb`) and audio verification (`YmbToWav`), please see the **[Music Tools Documentation](MusicTools.md)**.

## Hardware Signal Diagnostics

These scripts help validate the raw physical signals coming off the Atari 7800 cartridge edge connector before they reach the ROM or YM2149.

### `tools/Scripts/ValidateCartSignals.cs`
Validates physical signals coming off the edge connector.
- **Requirements**: `sigrok-cli` and a logic analyzer (e.g., `fx2lafw`).
- **Usage**: Run from the project root.
- **Target Signals**: Analyzes `PHI2`, `R/W`, `HALT`, and `A15` for stable transitions.

### `tools/Scripts/ValidateLogicSignals.cs` & `tools/Scripts/ValidateLatchEnable.cs`
Advanced timing diagnostics used to detect mid-cycle address bus noise or "shattered" pulses.
- **Timing Check**: Ensures `YM_LE`, `BDIR`, and `BC1` only trigger when the `PHI2` clock is stable.
- **BC1 Monitor**: Confirms the logic equations correctly identify register selection (A0=0) vs. data writes.

## ROM Packaging Tools

### `tools/A78Gen/Program.cs`
Generates a 128-byte A78 header and packages a raw binary for emulator use.
- **Status**: **Work in Progress**.
- **Planned Improvements**: Advanced mapping for varied cartridge header types and better support for hardware bank-switching.
