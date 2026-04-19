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

### `tools/Scripts/ValidateGalSignals.cs` & `tools/Scripts/ValidateLatchEnable.cs`
Advanced timing diagnostics used to detect mid-cycle address bus noise or "shattered" pulses.
- **Timing Check**: Ensures `YM_LE`, `BDIR`, and `BC1` only trigger when the `PHI2` clock is stable.
- **BC1 Monitor**: Confirms the GAL correctly identifies register selection (A0=0) vs. data writes.

### `tools/Scripts/ValidateYMSignals.cs`
Specifically monitors the input pins of the YM2149 to ensure the 74HCT373 latch is correctly delivering the data bus payload during the write window.
