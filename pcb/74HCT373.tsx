import { type ChipProps } from "tscircuit";

export const Latch74HCT373 = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip20_w300mil"
    pinLabels={{
      1: "OE",
      2: "Q0",
      3: "D0",
      4: "D1",
      5: "Q1",
      6: "Q2",
      7: "D2",
      8: "D3",
      9: "Q3",
      10: "GND",
      11: "LE",
      12: "Q4",
      13: "D4",
      14: "D5",
      15: "Q5",
      16: "Q6",
      17: "D6",
      18: "D7",
      19: "Q7",
      20: "VCC",
    }}
    schPinArrangement={{
      topSide: { pins: ["VCC"], direction: "left-to-right" },
      bottomSide: { pins: ["GND"], direction: "left-to-right" },
      leftSide: {
        pins: ["OE", "LE", "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7"],
        direction: "top-to-bottom"
      },
      rightSide: {
        pins: ["Q0", "Q1", "Q2", "Q3", "Q4", "Q5", "Q6", "Q7"],
        direction: "top-to-bottom"
      }
    }}
  />
);
