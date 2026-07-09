import { type ChipProps } from "tscircuit";

export const ATF16V8B = (props: ChipProps) => (
  <chip
    {...props}
    footprint="dip20_w300mil"
    pinLabels={{
      1: "CLK",
      2: "A15",
      3: "A14",
      4: "A0",
      5: "HALT",
      6: "RW",
      7: "PHI2",
      8: "A13",
      9: "A12",
      10: "GND",
      11: "A11",
      12: "NC",
      13: "NC",
      14: "NC",
      15: "YM_LE",
      16: "PHI2OUT",
      17: "BC1",
      18: "BDIR",
      19: "ROM_CE",
      20: "VCC",
    }}
    schPinArrangement={{
      topSide: { pins: ["VCC"], direction: "left-to-right" },
      bottomSide: { pins: ["GND"], direction: "left-to-right" },
      leftSide: {
        pins: ["CLK", "A15", "A14", "A13", "A12", "A11", "A0", "HALT", "RW", "PHI2"],
        direction: "top-to-bottom"
      },
      rightSide: {
        pins: ["ROM_CE", "YM_LE", "PHI2OUT", "BDIR", "BC1"],
        direction: "top-to-bottom"
      }
    }}
  />
);
