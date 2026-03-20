/*
  AY-3-8910 Arduino Bring-Up Test
  --------------------------------

  This sketch drives a real AY-3-8910 Programmable Sound Generator (PSG)
  from an Arduino (5V, 16 MHz) for hardware validation and experimentation.

  Purpose:
  - Verify that a standalone AY-3-8910 is functional.
  - Confirm correct wiring of data bus and control signals.
  - Provide a known-good reference before integrating into the Atari 7800 project.
  - Serve as a PSG “playground” for tone, noise, and envelope experiments.

  Hardware Assumptions:
  ---------------------
  AY-3-8910 connections:

    Pin 1       -> GND
    Pin 4       -> Channel A audio output
                -> 4.7k series resistor
                -> Line-level input of powered speaker
    Pin 23      -> RESET (held HIGH)
    Pin 27      -> BDIR (Arduino control pin)
    Pin 28      -> BC2 (held HIGH for bus decoding)
    Pin 29      -> BC1  (Arduino control pin)
    Pins 30–37  -> D7–D0 (Arduino digital outputs)
    Pin 40      -> +5V

  Critical Notes:
  ---------------
  - The AY requires a *continuous master clock*. It is divider-driven,
    not event-driven. If the clock stops, tone generation stops.
  - BC2 must be HIGH for address/data writes to be decoded.
  - Address latch cycle:
        BC1 = 1, BDIR = 1  (with BC2 = 1)
  - Data write cycle:
        BC1 = 0, BDIR = 1  (with BC2 = 1)
  - Clock must continue running while registers are updated.
  - Do NOT drive a raw 8Ω speaker directly from the AY output.

  What This Sketch Does:
  ----------------------
  - Initializes Channel A
  - Enables tone generator
  - Sets tone period
  - Sets max volume
  - Runs a continuous clock in loop()
  - Optionally sweeps tone values for testing

  This file is intentionally simple and timing-conservative.
  It is a hardware validation harness, not a music engine.

  Verified Working:
  -----------------
  - Stable tone generation
  - Proper register writes
  - Correct BC1/BDIR sequencing
  - Continuous clock requirement confirmed
*/

#define CLOCK_PIN 10
#define BDIR 14
#define BC1  15

int dataPins[8] = {2,3,4,5,6,7,8,9};

void setData(byte value) {
  for (int i = 0; i < 8; i++)
    digitalWrite(dataPins[i], (value >> i) & 1);
}

void latchAddress(byte reg) {
  setData(reg);
  delayMicroseconds(10);

  digitalWrite(BC1, HIGH);
  delayMicroseconds(10);
  digitalWrite(BDIR, HIGH);
  delayMicroseconds(10);
  digitalWrite(BDIR, LOW);
  delayMicroseconds(10);
  digitalWrite(BC1, LOW);
  delayMicroseconds(10);
}

void writeData(byte val) {
  setData(val);
  delayMicroseconds(10);

  digitalWrite(BDIR, HIGH);
  delayMicroseconds(10);
  digitalWrite(BDIR, LOW);
  delayMicroseconds(10);
}

void writeReg(byte reg, byte val) {
  latchAddress(reg);
  writeData(val);
}

void setup() {

  for (int i = 0; i < 8; i++) {
    pinMode(dataPins[i], OUTPUT);
    digitalWrite(dataPins[i], LOW);
  }

  pinMode(BDIR, OUTPUT);
  pinMode(BC1, OUTPUT);
  digitalWrite(BDIR, LOW);
  digitalWrite(BC1, LOW);

  pinMode(CLOCK_PIN, OUTPUT);

  delay(50);

  // Enable tone A only
  writeReg(7, 0b11111110);

  // Tone period
  writeReg(0, 0x40);
  writeReg(1, 0x00);

  // Volume max
  writeReg(8, 0x0F);
}

void loop() {
  // Continuous clock
  digitalWrite(CLOCK_PIN, HIGH);
  digitalWrite(CLOCK_PIN, LOW);  
}
