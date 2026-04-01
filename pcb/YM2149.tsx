import { type ChipProps } from "tscircuit";

export const YM2149 = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip40_w600mil"
    pinLabels={{
      1: "VSS",
      2: "TEST2",
      3: "ANALOG_A",
      4: "ANALOG_B",
      22: "CLK",
      23: "VCC",
      24: "ANALOG_C_REF",
      25: "VCC_ANALOG",
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
  />
);
