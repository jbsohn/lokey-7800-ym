# PCB Design & Automated Routing Pipeline

> [!WARNING]
> **Work in Progress**: The PCB design is in an experimental prototype phase and is **not production-ready**. The physical board has **not yet been ordered or tested** for fabrication.

This document explains the architecture of the Atari 7800 YM2149 sound card cartridge PCB, how the automated compilation and routing pipeline works, and the design decisions made to work around current `tscircuit` limitations.

> [!NOTE]
> **Why we use a custom post-routing pipeline:**
> This project uses **tscircuit** to code-define all component placements, schematic connections, and the initial unrouted PCB footprints. We only use KiCad as a final post-processing stage to complete trace routing, apply low-level DRC patches, and generate manufacturing files. 
> 
> Because the native `tscircuit` layout engine and design-rule configurations are currently optimized for simpler boards, our build runner (`pcb/route_and_patch.py`) programmatically exports the unrouted design, patches the board parameters using the KiCad Python API, and offloads the double-sided routing to `Freerouting`.
> 
> **Important**: KiCad is used purely as a post-processing tool to push the `tscircuit` design over the finish line. This project is **not** intended to be developed or edited manually in KiCad; any manual modifications will be overwritten upon the next build.
> 
> As `tscircuit` matures, the ultimate goal is to eliminate these external scripts and perform all design rule configurations and routing natively within the framework.

---

## PCB Overview
Each board is a **2-layer cartridge PCB** currently in the **experimental prototype phase (not production-ready)**. The project is still in its early stages, and neither board has been ordered for physical fabrication. While they interface the Atari 7800's expansion port to a YM2149 sound chip, address decoding logic, and audio pre-amplifier, mechanical verification and adjustments to fit standard cartridge shells remain a work-in-progress.

There are two board designs, defined as separate tscircuit entry files under `pcb/`:

* **`28pin.circuit.tsx`** — single YM2149, ATF16V8B PLD, solder-jumper ROM size selection. Full wiring/BOM: [Hardware-28pin.md](Hardware-28pin.md).
* **`32pin.circuit.tsx`** — single YM2149, ATF16V8B PLD, native DIP-32 socket with software bank switching via the YM IOA port. Full wiring/BOM: [Hardware-32pin.md](Hardware-32pin.md).

This document covers only the shared code-to-PCB pipeline; see the two hardware docs above for chip pinouts, jumper/bank-switching configuration, and per-board BOM.

### Board Previews:

> [!NOTE]
> These previews link to the [latest GitHub Release](https://github.com/jbsohn/lokey-7800-ym/releases/latest) build artifacts rather than files checked into this repo, so they always reflect the most recently tagged `v*` release (not necessarily the current `main` branch).

| Board | Front View (Top Copper & Silkscreen) | Back View (Bottom Copper & Silkscreen - Mirrored) |
| :--- | :---: | :---: |
| **28-pin** | ![28-pin PCB Front](https://github.com/jbsohn/lokey-7800-ym/releases/latest/download/pcb_front_28pin.svg) | ![28-pin PCB Back](https://github.com/jbsohn/lokey-7800-ym/releases/latest/download/pcb_back_28pin.svg) |
| **32-pin** | ![32-pin PCB Front](https://github.com/jbsohn/lokey-7800-ym/releases/latest/download/pcb_front_32pin.svg) | ![32-pin PCB Back](https://github.com/jbsohn/lokey-7800-ym/releases/latest/download/pcb_back_32pin.svg) |

---

## Compilation & Routing Pipeline

Because we maintain a code-first design, the single source of truth for each board is its own entry file — `pcb/28pin.circuit.tsx` or `pcb/32pin.circuit.tsx`. Generating the final routed KiCad project and manufacturing-ready Gerber/Drill files is automated via `make pcb-28pin` or `make pcb-32pin` (`make pcb` is an alias for `make pcb-32pin`).

To quickly regenerate the SVG visual board previews for documentation (using the currently compiled board design), you can run the standalone command:
```bash
make previews
```

To keep the pipeline robust, we interface directly with the **official KiCad Python API (`pcbnew`) and native `kicad-cli` commands** wherever possible rather than relying on custom text parsers.

The build runner (`pcb/route_and_patch.py <entry-file>.circuit.tsx`) executes the following sequential steps:

```mermaid
graph TD
    A[React Code *.circuit.tsx] -->|npx tsci export| B[Unrouted KiCad PCB]
    B -->|pcb/route_and_patch.py| C[Patched KiCad PCB & Rules]
    C -->|kicad-cli / python pcbnew| D[Specctra DSN Export]
    D -->|Boundary & Clearance Patch| E[Freerouting Session]
    E -->|Import SES| F[Routed KiCad PCB]
    F -->|kicad-cli DRC & Refill| G[Gerbers & Drill Files]
```

### Pipeline Steps:
1. **Export**: Compiles the React TSX file into an unrouted KiCad board (`.kicad_pcb`). We set `routingDisabled={true}` on the `<board>` in React to bypass the native layout engine, offloading the double-sided routing workload to the DSN/SES loop.
2. **Patching Design Settings (`route_and_patch.py`)**:
   * **Stubs**: Cleans up dummy `tscircuit:Unknown` footprint stubs.
   * **Silkscreen**: Standardizes reference designator text dimensions (height/width $\ge 0.8\text{mm}$, thickness $\ge 0.1\text{mm}$) to satisfy manufacturing silkscreen rules.
   * **DRC Severity**: Disables cosmetic warnings (e.g., missing footprints from libraries, text size out of range) in the project settings (`.kicad_pro`).
   * **Zone Filling**: Sets GND zones to always remove isolated copper islands (`SetIslandRemovalMode(0)`) and decreases zone `min_thickness` to `0.15mm`. This is a workaround that allows GND copper pours to flow continuously through the tight right-shoulder board notches, maintaining ground plane integrity.
   * **Custom Rules (`.kicad_dru`)**: Writes custom KiCad design rules to waive edge clearances specifically for the card-edge connector `J1` pads, allowing tight VCC/GND/signals to escape through the narrow connector notches.
3. **DSN Export**: Exports the board into Specctra DSN format using the `pcbnew` Python API.
4. **DSN Patching**:
   * Modifies clearance rules inside the DSN file so Freerouting handles the SMD edge pads correctly.
   * Pads the bottom DSN boundary coordinate to `-140.2mm` (instead of `-140.0mm`). This provides the minimum `0.2mm` clearance Freerouting needs to route the edge pins flush to the physical board edge without failing.
5. **Freerouting**: Launches the Freerouting CLI to automatically route all signals.
6. **Import**: Imports the generated Specctra SES route session back into the `.kicad_pcb` board.
7. **DRC & Zone Refill**: Refills all copper zones and executes `kicad-cli` Design Rule Checking.
8. **Export Gerbers**: Outputs production-ready plot files to `pcb/build/gerbers/`.

---

## Requirements & Build Instructions

> [!NOTE]
> Fabrication-ready Gerber files are **not** stored in this repository. A GitHub Actions workflow (`.github/workflows/release.yml`) automatically builds both boards and attaches Gerber archives (`gerbers-28pin.zip`, `gerbers-32pin.zip`) to each [GitHub Release](https://github.com/jbsohn/lokey-7800-ym/releases) — grab those if you just want to order the PCB. You only need to set up the dependencies below if you plan to modify the PCB code or rebuild the layout yourself.

### Requirements

1. **Node.js (v18+) & npm/bun**: Required to run the `tscircuit` React-to-PCB compiler.
2. **KiCad (v7.0 or v8.0+)**:
   - `kicad-cli` must be available in your system `PATH`.
   - The Python scripting environment (`pcbnew`) must be installed. On macOS, this is typically bundled inside the KiCad application. On Linux, install python3-kicad.
3. **Freerouting**:
   - Freerouting is a Java application, so a **Java Runtime Environment (JRE 21+)** must be installed and on `PATH`.
   - The documented way to run it is `java -jar freerouting-X.Y.Z.jar` (see the [official CLI docs](https://github.com/freerouting/freerouting/blob/master/docs/command_line_arguments.md)). Download a release jar and point `FREEROUTING_JAR` at it — this is the preferred method on **every platform, including macOS**.
   - Alternatively, if you already have a `freerouting` executable/wrapper installed and on `PATH` (e.g. a distro package), it'll be used as a fallback, or you can point `FREEROUTING_BIN` at it directly.

### Build Instructions

1. **Install Node dependencies**:
   Navigate to the `pcb/` directory and install the packages:
   ```bash
   cd pcb
   npm install
   ```

2. **Compile and Route the PCB**:
   From the repository root directory, run one of:
   ```bash
   make pcb-28pin
   make pcb-32pin   # same as `make pcb`
   ```
   Each target automates the entire pipeline for that board:
   - Runs `route_and_patch.py` to compile the React code, apply design tweaks, auto-route the traces using Freerouting, and run the final Design Rule Check (DRC).
   - Populates the production Gerber/Drill files in `pcb/build/gerbers/` and archives them as `pcb/build/gerbers.zip`.

   Run `make schematic-28pin` / `make schematic-32pin` to generate that board's schematic diagram (`docs/schematic-28pin.svg` / `docs/schematic-32pin.svg`), and `make previews` to export front/back board previews to `docs/pcb_front.svg` / `docs/pcb_back.svg` (shared filenames — these reflect whichever board was routed last; see note above).
