# YM Sound & Replayer Specification

This branch (`rust-refactor`) is an experimental branch to refactor and rewrite the project toolchain in Rust. The original C# tools in `tools-cs/` served as a validated proof-of-concept to answer a single question: *can an Atari ST YM music file actually play on a 7800 with a YM2149, compressed to fit within 32K?* 

The answer is an enthusiastic yes—it worked even better than expected, proving that high-quality chiptune playback is fully viable on the 7800's architecture by offloading the heavy analysis and compression to a modern workstation, leaving the 7800 to simply "stream" and decode the pre-compiled register delta data.

---

## Scope & Ingestion

The SDK compiles chiptune formats into custom, highly-compressed cartridge binary formats targeting the YM2149 Programmable Sound Generator (PSG) on the Atari 7800. We leverage the **`ym2149-rs`** crate workspace to support this wide array of formats with minimal custom parsing effort on our end.

### Supported Input Formats
*   **Music**:
    *   `.ym` (Atari ST YM2–YM6 register dumps) via `ym2149-ym-replayer`.
    *   `.aks` (Arkos Tracker 3 project XML files) via `ym2149-arkos-replayer`.
    *   `.ay` (ZX Spectrum Z80-emulated tracks) via `ym2149-ay-replayer`.
    *   `.sndh` (Atari ST 68000-emulated tracks) via `ym2149-sndh-replayer`.
*   **Sound Effects (SFX)**:
    *   `.yms` (Hand-authored YAML or inline-JSON source files).
    *   `.csv` (AYFXedit active-high columns visual export).

---

## Cartridge Binary Formats

Audio assets are compiled into optimized formats to fit within a **16KB bank size constraint** and run under a **<1% 6502 CPU cycle budget** (~298 cycles per frame).

### A. Music Format (`.ymb`)
*   **Pattern-based Delta Masking**: The song is divided into fixed-size pattern blocks.
*   **Pattern Independence**: The first frame of every pattern uses a full `0x3FFF` register mask to reset all 14 registers, eliminating inter-pattern dependencies and enabling $O(1)$ seeking/looping.
*   **Cross-Channel Deduplication**: Registers are split into three channel streams (Ch A, B, C) and one shared stream. Patterns are matched and deduplicated across all three channels independently, yielding 30% to 50% size savings.
*   **Unified Sequencer**: The sequence table maps indices to 4 pattern IDs (Ch A, B, C, Shared) to minimize 6502 pointer/index overhead.

### B. Sound Effects Format (`.ysb`)
*   **1-Byte Delta Mask**: Each frame uses a 1-byte mask representing changes to a single channel's parameters (volume, pitch, enables) + global noise parameters.
*   **Duration Multiplier**: Utilizes a `duration` key representing the total tick count a frame is held. This is encoded directly in the mask's upper bits, eliminating redundant hold frames.

---

## Playback & Channel Takeover Architecture

The replayer driver decodes the music stream into a 14-byte working RAM buffer unconditionally on every VBI tick, ensuring seamless resume when a sound effect ends.

```
                             [ Replayer VBI Update ]
                                        │
                                        ▼
                           ┌──────────────────────────┐
                           │ Decode Music to 14-byte  │
                           │     RAM Buffer (0-13)    │
                           └────────────┬─────────────┘
                                        │
                                        ▼
                           ┌──────────────────────────┐
                           │   Is SFX Active on any   │
                           │         channels?        │
                           └────────────┬─────────────┘
                                        │
                         ┌──────────────┴──────────────┐
                         │ Yes                         │ No
                         ▼                             ▼
            ┌──────────────────────────┐  ┌──────────────────────────┐
            │ Substitute Pitch/Volume/ │  │ Write 14-byte RAM Buffer │
            │ Mixer bits in RAM Buffer │  │    Directly to YM PSG    │
            └────────────┬─────────────┘  │      ($0800/$0801)       │
                         │                └──────────────────────────┘
                         ▼
            ┌──────────────────────────┐
            │ Resolve Global Conflicts │
            │ (Noise Period / Envelope)│
            └────────────┬─────────────┘
                         │
                         ▼
            ┌──────────────────────────┐
            │ Write 14-byte RAM Buffer │
            │    Directly to YM PSG    │
            │      ($0800/$0801)       │
            └──────────────────────────┘
```

### Global Register Arbitration
*   **Pitch & Volume (Channel-Isolated)**: Overridden unconditionally per active channel.
*   **Noise Period (R6)**: If an active SFX channel requests noise, it takes exclusive ownership of the global Noise Period (R6). The replayer suspends writing the music's R6 values and writes the SFX's requested R6 value instead.
*   **Envelopes (R11-R13)**: The hardware envelope generator remains reserved for music. Sound effects are restricted to software volume envelopes (manipulating volume R8-R10 over time) to prevent global audio distortion.

---

## Rust Workspace & Crates Selected

We have selected the following crates to form the core of our workspace:

*   **`ym2149-rs` (slippyex workspace)**: Modular chiptune emulation and parsing stack.
    *   `ym2149`: Core cycle-accurate PSG chip emulation.
    *   `ym2149-ym-replayer`: Decodes and plays legacy `.ym` files.
    *   `ym2149-arkos-replayer`: Natively parses Arkos Tracker project XML files (`.aks`).
    *   `ym2149-ay-replayer` & `ym2149-sndh-replayer`: Z80 and 68000 CPU simulators to capture Spectrum/Atari ST register streams.
*   **`mos6502`**: Standard-compliant, actively maintained NMOS 6502 CPU simulator for unit-testing compiled assembly drivers.
*   **`rodio` & `cpal`**: Audio playback backend (where `rodio` handles mixing and sinks, wrapping `cpal`'s low-level cross-platform hardware streams).
*   **`serde`, `serde_yaml`, & `serde_json`**: For parsing `.yms` YAML/JSON sound source files.
*   **`csv`**: For parsing visual AYFX `.csv` files.

These crates made Rust a very interesting option!
---

## Rust Refactor Roadmap & Core Milestones

The primary development roadmap for the new Rust-based SDK workspace consists of two core milestones:

*   **Milestone 1: `ymsfxtoysb` (Sound Effects Compiler & Player)**
    *   Parse `.yms` (JSON/YAML) and AYFX `.csv` files.
    *   Implement real-time workstation audio playback previewer using the `ym2149` chip emulator core and `rodio`/`cpal` output streaming.
    *   Compile sound effects into optimized `.ysb` target binaries using a 1-byte delta mask and the `duration` hold properties.
*   **Milestone 2: `ymtoymb` (Music Compiler)**
    *   Directly parse Arkos Tracker project XML files (`.aks`) and legacy `.ym`, `.ay`, and `.sndh` files.
    *   Apply compile-time pitch-scaling (Atari ST 2.0MHz $\rightarrow$ 7800 1.79MHz) and temporal resampling (50Hz $\rightarrow$ 60Hz NTSC).
    *   Implement **Cross-Channel Pattern Deduplication** and Sequence packing to meet the 16KB bank size constraint.

---

## "Crazy Stuff We Might Do" (Optional / Highly Drop-Friendly)

If we have too much caffeine or find ourselves with excess spare time, here is the wishlist of features we can easily throw out the window if reality catches up with us:

*   **Software-in-the-Loop (SIL) Matrix Mode**:
    *   *The Idea*: Run the actual compiled 6502 replayer code inside a virtual `mos6502` CPU simulator on the workstation. The Rust tool runs DASM/MADS in the background, loads the `.bin` into emulated RAM, intercepts memory writes to `$0800` / `$0801`, and plays them through the PC speakers. 
    *   *Steps*:
        1.  **Compile**: Rust harness runs DASM/MADS in the background.
        2.  **Load**: loads target `.bin` and `.ymb`/`.ysb` assets into virtual `mos6502` RAM.
        3.  **Bridge**: Simulates the 6502 CPU and redirects register writes to the emulated `ym2149` PSG core.
        4.  **Preview**: Emulated YM PSG core outputs audio PCM samples to the PC speakers via `rodio`/`cpal`.
*   **6502 Assembly Unit Testing**:
    *   *The Idea*: Write standard Rust unit tests that load specific compiled 6502 subroutines (e.g., bit-unpacking, volume scaling, or pointer calculation) into `mos6502` memory. The test sets initial registers/RAM values, steps the CPU, and asserts that the resulting register states and memory locations match expected values.
    *   *Status*: A highly practical way to debug low-level assembly logic (off-by-ones, register clobbering) headlessly.
