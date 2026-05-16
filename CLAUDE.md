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
- **Naming**: `snake_case` for labels and variables.
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
- **Testing**: Use `YmbToWav` to verify that bitmask compression is lossless.

### PCB Design (tscircuit)

- **Source of Truth**: `docs/Hardware.md` is the authoritative source for hardware pinouts, memory maps, and signal logic. The `pcb/` project and all related code must always match what is defined in the hardware documentation.
- **Workflow**: Code-driven React components in `pcb/*.tsx`.
- **Standards**: 6 mil trace/space for signals, 16 mil for power.
- **Components**: Prefer standard DIP packages for hobbyist ease-of-assembly.

## Project Structure

<<<<<<< HEAD

- `ca65/`: 6502 assembly sample code and reference player (ca65).
- `sample-code/`: Original DASM assembly samples.
- `docs/`: Technical reference and deep-dive guides.
- `tools/`: .NET conversion tools and Sigrok diagnostic scripts.
- `gal/`: Programmable logic (GAL16V8) sources.
- `pcb/`: tscircuit PCB design files.
- `ym-samples/`: Original Atari ST music sources.
- `vgm-samples/`: VGM/VGZ music sources.
=======
- `tools/`: .NET 10.0 source for conversion and diagnostic tools.
- `docs/`: Technical specifications, wiring diagrams, and hardware guides.
- `gal/`: Programmable logic (ATF16V8B) sources.
- `sample-code/`: DASM 6502 assembly for drivers and test ROMs.
- `ym-samples/` / `vgm-samples/`: Benchmark audio assets.
- `pcb/`: React-based PCB design using tscircuit.
- `build/`: Generated artifacts (ROMs, JEDs, WAVs).

>>>>>>> main
