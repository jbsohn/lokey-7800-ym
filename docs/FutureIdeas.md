# Future Ideas & Experimental Enhancements

This document captures long-term project roadmaps, experimental concepts, and hypothetical enhancements for the Lokey-YM project.

---

## 1. Hardware Supervisors & Co-Processors

### ATtiny85 Startup Supervisor (The "Chime" Module)
Adding a low-cost ATtiny85 microcontroller to act as a hardware supervisor during boot.
*   **Startup Chime**: Plays a signature computation sound or musical jingle immediately at power-on.
*   **Decryption Overlay**: Plays sound while the Atari 7800 BIOS is verifying the cartridge signature (which takes 2-3 seconds), giving the user audio feedback that the system is booting.
*   **Register Crush**: Performs a hardware-level silent reset of all YM2149 registers before the 6502 code begins execution.

### Independent Audio Co-Processor
Expanding the ATtiny supervisor logic to handle simple sound effects (SFX) or "achievement dings" independently of the main 6502 CPU, offloading basic audio tasks from the game loop.

---

## 2. YM2149 I/O Port Expansion

Since the YM2149's parallel I/O ports are unused on the 28-pin board and only partially used for bank-switching on the 32-pin board, they could drive future hardware expansions:

*   **Serial Debugging**: Bit-banging a UART protocol through an I/O pin to send real-time debug telemetry to a PC.
*   **External Flight Recorder**: Connecting to an Arduino or logic analyzer to record 6502 CPU bus transactions.
*   **Custom Controllers**: Interfacing SNES controllers or other non-standard inputs directly to the cartridge.
*   **MIDI/Synthesizer Interface**: Exposing I/O pins for MIDI-In control.
*   **Mass Storage (SPI)**: Interfacing with an SD card reader.
*   **Non-Volatile Save Storage (SPI FRAM/EEPROM)**: Bit-banging an SPI interface (SCK, MOSI, MISO, CS) using 4 I/O pins to read/write persistent save states, high scores, or game progress to an external SPI FRAM or EEPROM chip without needing a battery backup.

---

## 3. Physical Form Factor & Output

### Direct Audio Output Jack
Adding a dedicated 3.5mm Stereo Jack directly to the top of the cartridge.
*   **High Fidelity**: Bypasses the noisy RF/internal audio circuitry of the Atari 7800.
*   **Custom Stereo Mix**: Allows panning the three PSG channels across left/right channels.
