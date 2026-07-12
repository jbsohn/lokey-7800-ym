# Lokey 7800 YM

## Project Overview

The Atari 7800 community's favorite audio upgrades have a supply problem: POKEY clones run **~$10-$40 each — when they're in stock** — and the YM2151 has been out of production for decades, commanding collector prices used. The **Lokey 7800 YM** takes a different route: the **YM2149 PSG** — the Atari ST's sound chip — is still manufactured today as the **KC89C72** clone for **about $2**. One in-production chip brings three-channel sound to the 7800, and with it a direct bridge to four decades of Atari ST music and a tracker ecosystem that is still alive and composing. Original YM2149 chips are also plentiful as used or New-Old-Stock (NOS) parts.

### What's in a Name?

The **Lokey** name is a triple-layered nod to the project's roots:

* **POKEY**: A respectful "cousin" to Atari’s legendary sound chip.
* **Low-Key**: Reflecting our philosophy of a minimalist, stealthy, and cost-effective design.
* **Loki**: Inspired by the Norse god of mischief—bringing a bit of technical "trickery" to the Atari 7800 bus.

The project views the **Atari ST** as a potential **Creation System**.
 With established trackers (like Protracker ST or Maxymiser), it offers a path for audio production. Modern cross-platform tools like [**Furnace Tracker**](https://tildearrow.org/furnace) and [**Arkos Tracker**](https://www.julien-nevo.com/arkostracker/) (which is dedicated specifically to the AY/YM architecture) can also export VGM data for use with our tools on the 7800.

The **Atari 7800** acts as the **Consumer** of these assets. By bridging the hardware gap, we hope to allow the 7800 to play music from the ST era or new compositions from modern trackers.

### Design Goals

* **In-Production Parts Only**: No POKEY or YM2151 unobtainium, and no expensive FPGAs. Every part on the BOM should be sourceable new today (e.g. the KC89C72 YM2149 clone).
* **No SMD**: Hand-solderable through-hole components only, so hobbyists without a hot air rework station can build and repair boards.
* **Low Cost**: Keep the total build cost minimal.
* **Time-Period Accurate**: Stay faithful to what would have been technically feasible/authentic to the Atari 7800/ST era, rather than leaning on modern shortcuts.

### Hardware & Development Status

* **Working Prototype**: We have a hand-built cartridge prototype that has been verified to work.
* **PCB Effort**: We are working on a PCB design using a code-to-PCB workflow (**tscircuit**).
* **Reproducible Hardware Builds**: GitHub Actions rebuilds the PLD logic and both PCBs from source on every push. You can also build locally using the preconfigured **Docker Dev Container** (currently supported for building and routing PCBs).
* **Conversion Tools**: The project includes experimental tools to convert music files. The current C# implementation was a temporary hack to parse ST captures; this will be rewritten in C++ and optimized to support simultaneous music and sound effects playback.
* **SDK Progress**: We are working toward a set of tools and drivers to help others add YM music to their 7800 projects.
* **Custom Emulation**: For testing without hardware, we use forks of **a7800** and **js7800** that implement this specific memory mapping. *If this project gains momentum, we hope these changes might eventually be useful to the official upstream projects.*

## Status: STABLE ALPHA

We have achieved playback on physical Atari 7800 consoles using a bitmask-compressed register engine. The current implementation is designed to be efficient, leaving significant CPU time for other tasks.

## See it in Action

### Hardware Prototype

![Atari 7800 YM2149 Cartridge Prototype](docs/prototype.jpg)

### Web Emulator (Instant Play)

Test the bridge right now in your browser using our custom **js7800** fork.
👉 **[Play the YM2149 Demos in your Browser](https://jbsohn.github.io/js7800-ym-player/)**

### Real Hardware (ANCOOL1 Stress Test)

This video shows a physical Atari 7800 playing a full 92-second capture of the "ANCOOL1" track, filling 96% of a single 32KB ROM bank to prove bus stability and engine efficiency.

[![ANCOOL1 Stress Test on Atari 7800 Hardware](https://img.youtube.com/vi/LWzkfaaal2E/0.jpg)](https://www.youtube.com/shorts/LWzkfaaal2E)

## Documentation

* **[Development Guide](CLAUDE.md)** - Technical standards and build process for contributors and AI assistants.
* **[AI Development Agents](AGENTS.md)** - Specialized instructions for AI-assisted development and automation.
* **[SDK Samples & Building](docs/Samples.md)** - Compile ROMs and set up the environment.

* **[File Extension Reference](docs/FileExtensions.md)** - Guide to `.ymb`, `.ymi`, `.rom`, and `.a78` files.
* **[Hardware & Wiring](docs/Hardware.md)** - Shared memory mapping and connector/chip pinouts.
  * **[28-Pin Board](docs/Hardware-28pin.md)** - Single-YM2149, jumper-configured ROM board.
  * **[32-Pin Board](docs/Hardware-32pin.md)** - Bank-switched ROM board (fixed 32KB code + 16KB YM-IOA banked data window, 128KB-256KB) (Experimental Pre-Alpha v0.3 - Untested).

* **[PCB Design (tscircuit)](docs/PCB.md)** - Code-to-PCB workflow.

* **[Emulator Support](docs/Emulation.md)** - Using `a7800` and `js7800` for development.

* **[Toolchain & Diagnostics](docs/Tools.md)** - Register conversion (`YmToYmb`, `VgmToYmb`), audio verification (`YmbToWav`), and hardware signal verification.
* **[YMB Format](docs/YmbFormat.md)** - Technical specification of the music binary format.
* **[Musical Credits](docs/Musicians.md)** - Honoring the original composers.
* **[TODO](docs/TODO.md)** - Known issues and improvements.
* **[Future Ideas](docs/FutureIdeas.md)** - Roadmap for hardware/software expansions.

---

## Technical Highlights

* **Hardware**: Uses **YM2149 PSG** mapped to `$0800`/`$0801` (Pokey800-compatible).
* **Compression**: Multi-stage **Pattern-Based Delta Masking** for maximum ROM efficiency.
* **Timing**: Automatic **Pitch Scaling** (1.79MHz vs 2.0MHz) ensures tracks stay in tune.
* **Diagnostics**: Built-in **Software Triad** visualizer for bare-metal debugging.

## Acknowledgements & Credits

* **Karri Kaksonen (karrika)**: For the excellent [Otaku-flash](https://github.com/karrika/Otaku-flash) project. We have integrated the **Stable Alpha** Atari 7800 cartridge footprints, symbols, and professional design rules from this MIT-licensed repository.
* **Simon Frankau ([galette](https://github.com/simon-frankau/galette))**: For the open-source **galette** logic assembler. It provides a modern, cross-platform toolchain for compiling ATF16V8B/ATF22V10 logic, saving us from the legacy Windows tools.
* **Dan Boris (AtariHQ)**: For the indispensable [7800 Cartridge Technical Specifications](https://atarihq.com/danb/7800cart/a7800cart.shtml) and reference diagrams that made this hardware mapping possible.
* **Olivier PONCET (aym-js)**: For the high-fidelity [aym-js](https://github.com/ponceto/aym-js) YM2149 emulator core. Our `BinToWav` tool uses a literal C# port of this logic to ensure high-accuracy verification of music assets.

* **Arnaud Carré (Leonard/OXG)**: For the excellent [StSound](https://github.com/arnaud-carre/StSound) project. The melodic assets used for hardware testing were sourced via this project's research.

* **Eagle & Ecernosoft**: For the insightful ideas and technical tips provided on the forums, including the Pokey800 mapping recommendation and the inspiration for the "Active Shunt" audio stage design.

* **Original Musicians & Composers**: For the timeless tracks used as benchmarks for this project. See **[Musical Credits](docs/Musicians.md)** for a full list of artists.

* **The Atari Community**: We are grateful to the dedicated fans keeping the 16-bit and 8-bit flames alive through archival and homebrew development.

## Contributing

This project is completely open source, and since this is a first-time PCB design, community feedback and contributions are highly welcome! *"Help! I need someone... not just anybody... you know I need someone!"* as the Beatles said, so any help or review of the schematics, code, or board layout is greatly appreciated.

## AI Assistance

This project was developed with assistance from AI. For the author, AI has been a "force multiplier"—making it possible to tackle long-held "I've always wanted to do this" projects within the limited hours of evenings and weekends. In particular, it really helped with developing the [route_and_patch.py](file:///home/john/Projects/7800-ym2149-lab/pcb/route_and_patch.py) build pipeline.

For reflections on the legacy of the Atari ST and how AI is changing the landscape of hardware and software side-projects, see the author's blog posts:

[John's Music & Tech](https://johnsmusicandtech.com/)

## License

This project uses a split license:

* **PCB design** (`pcb/`): Licensed under the **GNU General Public License v2.0 (GPL-2.0)**. See `pcb/LICENSE` for details.
* **Everything else** (assembly, tools, docs, PLD sources): Licensed under the **MIT License**. See `LICENSE` for details.

**Use at your own risk.** The author is not responsible for any damage to your hardware or loss of data.
