import { type ChipProps } from "tscircuit";

// Pin map matches pld/rom_ym_32pin.pld (GAL22V10: GND=12, VCC=24,
// pins 14-23 are I/O macrocells; pin 14 is used as the IOA3 input)
export const ATF22V10 = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip24_w300mil"
    pinLabels={{
      1: "PHI2",
      2: "A15",
      3: "A14",
      4: "A13",
      5: "A12",
      6: "A11",
      7: "A0",
      8: "RW",
      9: "HALT",
      10: "IOA0",
      11: "IOA1",
      12: "GND",
      13: "IOA2",
      14: "IOA3",
      15: "ROM_A14",
      16: "ROM_A15",
      17: "ROM_A16",
      18: "ROM_A17",
      19: "ROM_CE",
      20: "BC1",
      21: "BDIR",
      22: "PHI2OUT",
      23: "YM_LE",
      24: "VCC",
    }}
    schPinArrangement={{
      topSide: { pins: ["VCC"], direction: "left-to-right" },
      bottomSide: { pins: ["GND"], direction: "left-to-right" },
      leftSide: {
        pins: ["PHI2", "A15", "A14", "A13", "A12", "A11", "A0", "RW", "HALT", "IOA0", "IOA1", "IOA2", "IOA3"],
        direction: "top-to-bottom",
      },
      rightSide: {
        pins: ["ROM_CE", "YM_LE", "PHI2OUT", "BDIR", "BC1", "ROM_A14", "ROM_A15", "ROM_A16", "ROM_A17"],
        direction: "top-to-bottom",
      },
    }}
  />
);
