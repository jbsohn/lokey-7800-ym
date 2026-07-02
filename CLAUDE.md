# CLAUDE.md

## Build Commands

- Build tools, ROMs, and `.a78` files: `make all`
- Build only .NET tools: `make tools`
- Build only logic ROMs and signed binaries: `make rom`
- Build only emulator-ready `.a78` files: `make a78`
- Build JEDEC files from `gal/*.pld` sources: `make logic`
- Generate verification WAV files: `make wav`
- Run specific tool: `dotnet run --project tools/<ToolName>/<ToolName>.csproj -- <args>`
- Clean all build artifacts: `make clean`

## Code Style & Standards

### 6502 Assembly (DASM)

- **Formatting**: 8-space indentation for instructions, 0-space for labels.
- **Naming**: 
  - `UPPER_CASE` for constants, offsets, and hardware registers (e.g., `MSTAT`, `NUM_REGS`).
  - `snake_case` for labels, RAM variables, and code (e.g., `play_frame`, `music_ptr`).
- **Vectors**: All ROMs must include standard Atari 7800 vectors at the end.
- **Memory Map**:
  - YM2149 Address Register: `$4000`
  - YM2149 Data Register: `$4001`
  - ROM Start: `$8000` (for 32KB images)

### C# / .NET

- **Version**: .NET 10.0+
- **Style**: Standard C# conventions (PascalCase for classes/methods, camelCase for local variables).
- **Architecture**: Core logic resides in `tools/Core/`, utilized by CLI wrappers.
- **Performance**: Use `ReadOnlySpan<byte>` for binary parsing where possible.
- **Testing**: Use `ymbtowav` to verify that bitmask compression is lossless.

### PCB Design (tscircuit)

- **Source of Truth**: `docs/Hardware.md` is the authoritative source for hardware pinouts, memory maps, and signal logic. The `pcb/` project and all related code must always match what is defined in the hardware documentation.
- **Workflow**: Code-driven React components in `pcb/*.tsx`.
- **Standards**: 6 mil trace/space for signals, 16 mil for power.
- **Components**: Prefer standard DIP packages for hobbyist ease-of-assembly.

## Project Structure

- `ca65/`: 6502 assembly sample code and reference player (ca65).
- `examples/`: Original DASM assembly samples.
- `docs/`: Technical reference and deep-dive guides.
- `tools/`: .NET conversion tools and Sigrok diagnostic scripts.
- `gal/`: Programmable logic (ATF16V8B / ATF22V10 PLD) sources.
- `pcb/`: tscircuit PCB design files.
- `ym-samples/`: Original Atari ST music sources.
- `vgm-samples/`: VGM/VGZ music sources.
