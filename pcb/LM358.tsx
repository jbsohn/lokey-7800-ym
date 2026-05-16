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
  />
);
