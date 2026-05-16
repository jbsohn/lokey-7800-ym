# Crazy Future Ideas & Enhancements

This document captures project roadmaps, experimental ideas, and definitely crazy enhancements for the Lokey-YM project.

## 1. Software & Toolchain Transition (IN PROGRESS)
**Status**: MADS selected; implementation in progress on separate branch.

We are transitioning from the initial DASM-based rapid prototyping environment to the **MADS** toolchain. 

### Why MADS:
*   **Best-in-Class**: Widely considered one of the best Atari assemblers available.
*   **Advanced Features**: Offers powerful macros, better bank-switching support, and flexible linking options required for "Pro" level 7800 development.

## 2. Pokey800 Memory Mapping Standard
The MVP currently uses the "Classic Pokey" decoding scheme (pioneered by *Ballblazer* and *Commando*), which mirrors the sound chip across the entire **$4000–$7FFF** range. 

### Enhancement:
Future versions could move to the **Pokey800** mapping standard.
*   **Why**: Modern 7800 homebrew feedback suggests that broad 16KB mirroring can interfere with other advanced expansions. 
*   **The Goal**: Refine the logic equations to use a narrower, more specific address window (often just a few bytes) to match the Pokey800 standard. This would allow the Lokey-YM to coexist with other high-end hardware in a single system.

## 3. ATtiny85 Startup Supervisor (The "Chime" Module)
**Status**: Examined, but put on hold for MVP.

One of the most unique ideas for the Lokey-YM is adding an **ATtiny85** microcontroller to act as a hardware supervisor. 

### Why it's cool:
*   **Startup Chime**: It can play a "70s Mainframe" computation sound or a musical jingle the moment the power is turned on.
*   **Autonomous Operation**: It can play sound while the Atari 7800 BIOS is still busy verifying the cartridge's digital signature.
*   **Pro Reliability**: It performs a "Register Crush" at boot, ensuring the YM2149 is perfectly silent and reset before the game starts.

## 4. RSA Checksum "Progress Bar" Audio
Using the ATtiny supervisor to specifically time its bleeps and chirps to match the Atari 7800's decryption window (approx. 2-3 seconds). This gives the user a "Progress Bar for the ears" so they know the console is working.

## 5. Independent Audio Co-Processor
Expanding the ATtiny logic to allow it to play simple sound effects (SFX) or "achievement dings" independently of the 6502 CPU, allowing the game to offload simple audio tasks.

## 6. YM2149 I/O Port Expansion
The YM2149 includes two 8-bit parallel I/O ports (Port A and Port B) that offer a variety of "hardware expansion" possibilities.

### Potential Uses:
*   **Hardware Bank-Switching**: Using the I/O pins to drive higher address lines on a larger ROM (e.g., 128KB or 512KB).
*   **Serial Debugging**: Bit-banging a UART protocol through an I/O pin to send debug data to a modern PC serial terminal.
*   **External Debugger Interface**: Connecting to an Arduino to act as a "flight recorder" for the 6502 CPU.
*   **Custom Controller Interface**: Connecting modern or non-standard game controllers (e.g., SNES pads) directly to the cartridge.
*   **MIDI Interface**: Allowing the Atari 7800 to act as a hardware synthesizer.
*   **SD Card Storage (SPI)**: Interfacing with an SD card reader for massive data storage.
*   **WiFi / Internet Connectivity**: Connecting an ESP8266 module for online leaderboards.
*   **Keyboard & Mouse Support (PS/2)**: Interfacing with standard PS/2 peripherals.
*   **Multiplayer Link Cable**: Connecting two Atari 7800 consoles together.

## 7. Direct Audio Output Jack
A dedicated **3.5mm Stereo Jack** directly on the top of the cartridge.

### Benefits:
*   **High Fidelity**: Bypasses the noisy internal circuitry of the 7800.
*   **True Stereo**: Allows for a custom stereo-mix of the three PSG channels.
