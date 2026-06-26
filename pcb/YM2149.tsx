import { type ChipProps } from "tscircuit";

export const YM2149 = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip40_w600mil"
    pinLabels={{
      1: "GND",
      3: "ANALOG_B",
      4: "ANALOG_A",
      6: "IOB7",
      7: "IOB6",
      8: "IOB5",
      9: "IOB4",
      10: "IOB3",
      11: "IOB2",
      12: "IOB1",
      13: "IOB0",
      14: "IOA7",
      15: "IOA6",
      16: "IOA5",
      17: "IOA4",
      18: "IOA3",
      19: "IOA2",
      20: "IOA1",
      21: "IOA0",
      22: "CLK",
      23: "RESET",
      24: "A9",
      25: "A8",
      27: "BDIR",
      28: "BC2",
      29: "BC1",
      30: "DA7",
      31: "DA6",
      32: "DA5",
      33: "DA4",
      34: "DA3",
      35: "DA2",
      36: "DA1",
      37: "DA0",
      38: "ANALOG_C",
      40: "VCC",
    }}
    schPinArrangement={{
      topSide: { pins: ["VCC"], direction: "left-to-right" },
      bottomSide: { pins: ["GND"], direction: "left-to-right" },
      leftSide: {
        pins: ["RESET", "CLK", "BDIR", "BC1", "BC2", "A8", "A9",
               "DA0", "DA1", "DA2", "DA3", "DA4", "DA5", "DA6", "DA7",
               "IOA0", "IOA1", "IOA2", "IOA3", "IOA4", "IOA5", "IOA6", "IOA7"],
        direction: "top-to-bottom",
      },
      rightSide: {
        pins: ["ANALOG_A", "ANALOG_B", "ANALOG_C",
               "IOB0", "IOB1", "IOB2", "IOB3", "IOB4", "IOB5", "IOB6", "IOB7"],
        direction: "top-to-bottom",
      },
    }}
  />
);
