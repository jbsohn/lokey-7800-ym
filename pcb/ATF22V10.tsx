import { type ChipProps } from "tscircuit";

export const ATF22V10 = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip24_w300mil"
    pinLabels={{
      1: "HALT",
      2: "A15",
      3: "A14",
      4: "A13",
      5: "A12",
      6: "A11",
      7: "A0",
      8: "RW",
      9: "PHI2",
      10: "IOA0",
      11: "IOA1",
      12: "IOA2",
      13: "IOA3",
      14: "GND",
      15: "YM_LE",
      16: "PHI2OUT",
      17: "BC1",
      18: "BDIR",
      19: "ROM_CE",
      20: "ROM_A15",
      21: "ROM_A16",
      22: "ROM_A17",
      23: "ROM_A18",
      24: "VCC",
    }}
    schPinArrangement={{
      topSide: { pins: ["VCC"], direction: "left-to-right" },
      bottomSide: { pins: ["GND"], direction: "left-to-right" },
      leftSide: {
        pins: ["HALT", "A15", "A14", "A13", "A12", "A11", "A0", "RW", "PHI2", "IOA0", "IOA1", "IOA2", "IOA3"],
        direction: "top-to-bottom",
      },
      rightSide: {
        pins: ["ROM_CE", "YM_LE", "PHI2OUT", "BDIR", "BC1", "ROM_A15", "ROM_A16", "ROM_A17", "ROM_A18"],
        direction: "top-to-bottom",
      },
    }}
  />
);
