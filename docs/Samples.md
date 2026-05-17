# SDK Samples & Building Guide

This guide covers the core sample applications included with the Lokey-YM SDK and provides instructions on how to build them.

## 1. Prerequisites

To build the samples and music tracks, you need the following:

* **Assembler**: Either **DASM** or **Mad Assembler (MADS)**.
* **.NET SDK**: Required for the diagnostic and processing tools. These have been verified on Linux and macOS.

---

## 2. The Core Samples

The samples are located in the `examples/` directory and demonstrate different aspects of YM2149 sound production on the Atari 7800.

### Universal Samples (DASM & MADS)

These use a "Universal Subset" of 6502 assembly compatible with both assemblers.

* **Universal Triad (`triad.asm`)**: A bare-metal diagnostic test that plays a C-Major triad (C3, E3, G3). Shows direct register writes to `$4000`/`$4001`.
* **Universal Player (`player.asm`)**: The core engine for playing compressed `.ymb` files with high-precision fractional timing.

### MADS "Pro" Sample (MADS Only)

These showcase high-level features available exclusively in **Mad Assembler (MADS)**.

* **MADS Pro Triad (`triad_mads.asm`)**: A modernized version of the triad demo using encapsulated procedures (`.PROC`), register-based parameter passing (`.REG`), and ergonomic moves (`MVA`).

---

## 3. Build Instructions

The SDK supports a dual-assembler workflow via the root `Makefile`.

### Build Commands

| Target | Command | Description |
| :--- | :--- | :--- |
| **Emulator (DASM)** | `make a78` | Build the universal samples and music using DASM. |
| **Emulator (MADS)** | `make a78 ASSEMBLER=mads` | Build the universal samples and music using MADS. |
| **Pro Demo** | `make pro` | Build the MADS-exclusive "Pro" triad demo. |
| **Hardware ROMs** | `make rom` | Build and sign raw 32K binary images for EPROMs. |
| **Verification** | `make wav` | Generate `.wav` files for all music tracks. |
| **Cleanup** | `make clean` | Remove all generated build artifacts. |

### Build Results
All commands output to the `build/` directory:
- **`.a78`**: Packed ROM with a 128-byte header for emulators (a7800, js7800).
  > [!NOTE]
  > The `A78Gen` tool used for header generation is a **work in progress**. Future updates will provide improved mapping for complex cartridge header types and bank-switching configurations.
- **`.rom`**: Raw 32,768-byte binary image, signed for hardware.


---

## 4. Hardware Deployment

### Signing for Real 7800 Hardware

Atari 7800 cartridges must be signed. While `make rom` handles this automatically, you can sign a raw binary manually:

```bash
7800sign -w build/your_app.rom
7800sign -t build/your_app.rom
```

### ROM Footer Requirements

For the signature to be valid, the ROM must have a specific 8-byte footer starting at `$FFF8`. In the SDK samples, this is handled natively for both assemblers.

For more details on writing compatible code, see the **[Universal Assembly Guidelines](UniversalAssemblyGuidelines.md)**.
