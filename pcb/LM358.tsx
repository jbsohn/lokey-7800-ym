import { type ChipProps } from "tscircuit";

export const LM358 = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip8_w300mil"
    pinLabels={{
      1: "OUT1",
      2: "IN1_NEG",
      3: "IN1_POS",
      4: "GND",
      5: "IN2_POS",
      6: "IN2_NEG",
      7: "OUT2",
      8: "VCC",
    }}
    schPinArrangement={{
      topSide: { pins: ["VCC"], direction: "left-to-right" },
      bottomSide: { pins: ["GND"], direction: "left-to-right" },
      leftSide: {
        pins: ["IN1_POS", "IN1_NEG", "IN2_POS", "IN2_NEG"],
        direction: "top-to-bottom"
      },
      rightSide: {
        pins: ["OUT1", "OUT2"],
        direction: "top-to-bottom"
      }
    }}
  />
);
