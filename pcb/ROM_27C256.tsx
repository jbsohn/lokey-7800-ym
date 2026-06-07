import { type ChipProps } from "tscircuit";

export const ROM_27C256 = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip28_w600mil"
    pinLabels={{
      1: "VPP",
      2: "A12",
      3: "A7",
      4: "A6",
      5: "A5",
      6: "A4",
      7: "A3",
      8: "A2",
      9: "A1",
      10: "A0",
      11: "D0",
      12: "D1",
      13: "D2",
      14: "GND",
      15: "D3",
      16: "D4",
      17: "D5",
      18: "D6",
      19: "D7",
      20: "CE",
      21: "A10",
      22: "OE",
      23: "A11",
      24: "A9",
      25: "A8",
      26: "A13",
      27: "A14",
      28: "VCC",
    }}
    schPinArrangement={{
      topSide: { pins: ["VCC", "VPP"], direction: "left-to-right" },
      bottomSide: { pins: ["GND"], direction: "left-to-right" },
      leftSide: {
        pins: ["CE", "OE", "A14", "A13", "A12", "A11", "A10", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "A1", "A0"],
        direction: "top-to-bottom"
      },
      rightSide: {
        pins: ["D7", "D6", "D5", "D4", "D3", "D2", "D1", "D0"],
        direction: "top-to-bottom"
      }
    }}
  />
);
