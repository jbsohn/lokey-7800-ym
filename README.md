# Lokey 7800 YM

## Project Overview

The **Lokey 7800 YM** project is an experiment in providing a low-cost (~$2 USD) bridge between the Atari 7800 and the Atari ST. By using the YM2149 PSG (or modern clones like the **KC89C72**), we aim to bring three-channel sound to the 7800 with Atari ST asset compatibility. Original YM2149 chips are also often available as used or New-Old-Stock (NOS) parts.

The project views the **Atari ST** as a potential **Creation System**. With established trackers (like Protracker ST or Maxymiser), it offers a path for audio production. Modern cross-platform tools like [**Furnace Tracker**](https://tildearrow.org/furnace) and [**Arkos Tracker**](https://www.julien-nevo.com/arkostracker/) (which is dedicated specifically to the AY/YM architecture) can also export VGM data for use with our tools on the 7800.

The **Atari 7800** acts as the **Consumer** of these assets. By bridging the hardware gap, we hope to allow the 7800 to play music from the ST era or new compositions from modern trackers.

### Hardware & Development Status

* **Working Prototype**: We have a hand-built cartridge prototype that has been verified to work.
* **PCB Effort**: We are working on a PCB design using a code-to-PCB workflow (**tscircuit**).
* **Conversion Tools**: The project includes tools to convert Atari ST **YM** and multi-platform **VGM** files into a format for the 7800.
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

---

## Documentation

- **[Development Guide](DEVELOPMENT.md)** - Technical standards and build process.
- **[Building & Toolchain](docs/Building.md)** - Compile ROMs and set up the environment.
* **[File Extension Reference](docs/FileExtensions.md)** - Guide to `.ymb`, `.ymi`, `.rom`, and `.a78` files.
* **[Hardware & Wiring](docs/Hardware.md)** - Pinouts, memory mapping, and GAL logic.

- **[PCB Design (tscircuit)](docs/PCB.md)** - Code-to-PCB workflow and **[Schematic PDF](docs/schematic.pdf)**.

* **[Emulator Support](docs/Emulation.md)** - Using `a7800` and `js7800` for development.

- **[Diagnostic Tools](docs/Tools.md)** - Hardware signal and timing verification.
* **[Music Tools](docs/MusicTools.md)** - Theory and usage for `YmToYmb`, `VgmToYmb`, and `YmbToWav`.
* **[YMB Format](docs/YmbFormat.md)** - Technical specification of the music binary format.
* **[Musical Credits](docs/Musicians.md)** - Honoring the original composers.

---

## Technical Highlights

* **Hardware**: Uses **YM2149 PSG** mapped to `$4000-$7FFF`.
* **Compression**: Multi-stage **Pattern-Based Delta Masking** for maximum ROM efficiency.
* **Timing**: Automatic **Pitch Scaling** (1.79MHz vs 2.0MHz) ensures tracks stay in tune.
* **Diagnostics**: Built-in **Software Heartbeat** visualizer for bare-metal debugging.

## Future Plans

* Support for complex bank-switching (beyond 32KB).
* High-speed diagnostic logging via YM I/O ports.

## Acknowledgements & Credits

* **Karri Kaksonen (karrika)**: For the excellent [Otaku-flash](https://github.com/karrika/Otaku-flash) project. We have integrated the **Stable Alpha** Atari 7800 cartridge footprints, symbols, and professional design rules from this MIT-licensed repository.
* **Simon Frankau ([galette](https://github.com/simon-frankau/galette))**: For the open-source **galette** GAL assembler. It provides a modern, cross-platform toolchain that saved us from the Windows VM nightmare of WinCUPL.
* **Dan Boris (AtariHQ)**: For the indispensable [7800 Cartridge Technical Specifications](https://atarihq.com/danb/7800cart/a7800cart.shtml) and reference diagrams that made this hardware mapping possible.
* **Olivier PONCET (aym-js)**: For the high-fidelity [aym-js](https://github.com/ponceto/aym-js) YM2149 emulator core. Our `BinToWav` tool uses a literal C# port of this logic to ensure high-accuracy verification of music assets.
- **Arnaud Carré (Leonard/OXG)**: For the excellent [StSound](https://github.com/arnaud-carre/StSound) project. The melodic assets used for hardware testing were sourced via this project's research.
- **Original Musicians & Composers**: For the timeless tracks used as benchmarks for this project. See **[Musical Credits](docs/Musicians.md)** for a full list of artists.

- **The Atari Community**: We are grateful to the dedicated fans keeping the 16-bit and 8-bit flames alive through archival and homebrew development.

## AI Assistance

This project was developed with assistance from AI. For the author, AI has been a "force multiplier"—making it possible to tackle long-held "I've always wanted to do this" projects within the limited hours of evenings and weekends.

For reflections on the legacy of the Atari ST and how AI is changing the landscape of hardware and software side-projects, see the author's blog posts:

[John's Music & Tech](https://johnsmusicandtech.com/)

## License

This project is licensed under the **GNU General Public License v2.0 (GPL-2.0)**. See `LICENSE` for details.

**Use at your own risk.** The author is not responsible for any damage to your hardware or loss of data.
