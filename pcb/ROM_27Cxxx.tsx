import { type ChipProps } from "tscircuit";

// Compatible with AT27C010 (128KB), AT27C020 (256KB), AT27C040 (512KB).
// Pin 30 (A17) is NC on 27C010. Pin 31 (A18) is PGM on 27C010/27C020.
export const ROM_27Cxxx = (props: ChipProps) => (
    <chip
      {...props}
      footprint="dip32_w600mil"
      pinLabels={{
        1: "VPP",
        2: "A16",
        3: "A15",
        4: "A12",
        5: "A7",
        6: "A6",
        7: "A5",
        8: "A4",
        9: "A3",
        10: "A2",
        11: "A1",
        12: "A0",
        13: "D0",
        14: "D1",
        15: "D2",
        16: "GND",
        17: "D3",
        18: "D4",
        19: "D5",
        20: "D6",
        21: "D7",
        22: "CE",
        23: "A10",
        24: "OE",
        25: "A11",
        26: "A9",
        27: "A8",
        28: "A13",
        29: "A14",
        30: "A17",
        31: "A18",
        32: "VCC",
      }}
      schPinArrangement={{
        topSide: { pins: ["VCC", "VPP"], direction: "left-to-right" },
        bottomSide: { pins: ["GND"], direction: "left-to-right" },
        leftSide: {
          pins: ["CE", "OE", "A18", "A17", "A16", "A15", "A14", "A13", "A12", "A11", "A10", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "A1", "A0"],
          direction: "top-to-bottom",
        },
        rightSide: {
          pins: ["D7", "D6", "D5", "D4", "D3", "D2", "D1", "D0"],
          direction: "top-to-bottom",
        },
      }}
    />
);
