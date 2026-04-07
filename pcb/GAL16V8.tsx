import { type ChipProps } from "tscircuit";

export const GAL16V8 = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip20_w300mil"
    pinLabels={{
      1: "CLK",
      2: "I1",
      3: "I2",
      4: "I3",
      5: "I4",
      6: "I5",
      7: "I6",
      8: "I7",
      9: "I8",
      10: "GND",
      11: "OE",
      12: "O8",
      13: "O7",
      14: "O6",
      15: "O5",
      16: "O4",
      17: "O3",
      18: "O2",
      19: "O1",
      20: "VCC",
    }}
  />
);
