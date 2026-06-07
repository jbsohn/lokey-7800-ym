import { type ChipProps } from "tscircuit";

export const YM2149 = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip40_w600mil"
    pinLabels={{
      1: "GND",
      3: "ANALOG_B",
      4: "ANALOG_A",
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
        pins: ["RESET", "CLK", "BDIR", "BC1", "BC2", "A8", "A9", "DA0", "DA1", "DA2", "DA3", "DA4", "DA5", "DA6", "DA7"],
        direction: "top-to-bottom"
      },
      rightSide: {
        pins: ["ANALOG_A", "ANALOG_B", "ANALOG_C"],
        direction: "top-to-bottom"
      }
    }}
  />
);
