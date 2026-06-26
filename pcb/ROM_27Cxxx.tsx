import { type ChipProps } from "tscircuit";

// Compatible with AT27C010 (128KB), AT27C020 (256KB), AT27C040 (512KB).
// Pin 30 (A17) is NC on 27C010. Pin 31 (A18) is PGM on 27C010/27C020.
// For 28-pin 27C256: insert at +2 offset (chip pin 1 at socket hole 3).
export const ROM_27Cxxx = (props: ChipProps) => {
  const PITCH = 2.54;
  const ROW_X = 7.62;   // 600mil / 2

  // Pin y-positions: pin 1 at top of left row (positive Y)
  const pinY = (n: number) => (15 / 2) * PITCH - (n - 1) * PITCH;

  // 28-pin chip at +2 offset: pin 1 lands at socket hole 3
  const BODY_X = ROW_X + 1.5;

  return (
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
    >
      <footprint>
        {/* 28-pin IC silkscreen outline (27C256 at +2 offset: pin 1 → socket hole 3) */}
        <silkscreenpath
          route={[
            { x: -BODY_X, y: pinY(3) },
            { x: -1.2, y: pinY(3) },
          ]}
          strokeWidth={0.12}
        />
        <silkscreenpath
          route={[
            { x: 1.2, y: pinY(3) },
            { x: BODY_X, y: pinY(3) },
          ]}
          strokeWidth={0.12}
        />
        <silkscreenpath
          route={[
            { x: -BODY_X, y: pinY(16) },
            { x: BODY_X, y: pinY(16) },
          ]}
          strokeWidth={0.12}
        />
        <silkscreenpath
          route={[
            { x: -BODY_X, y: pinY(3) },
            { x: -BODY_X, y: pinY(16) },
          ]}
          strokeWidth={0.12}
        />
        <silkscreenpath
          route={[
            { x: BODY_X, y: pinY(3) },
            { x: BODY_X, y: pinY(16) },
          ]}
          strokeWidth={0.12}
        />
        {/* Notch half-circle arc — curves inward into the body */}
        <silkscreenpath
          route={[
            { x: -1.2, y: pinY(3) },
            { x: -1.04, y: pinY(3) - 0.6 },
            { x: -0.6, y: pinY(3) - 1.04 },
            { x: 0, y: pinY(3) - 1.2 },
            { x: 0.6, y: pinY(3) - 1.04 },
            { x: 1.04, y: pinY(3) - 0.6 },
            { x: 1.2, y: pinY(3) },
          ]}
          strokeWidth={0.12}
        />
        <silkscreencircle pcbX={-ROW_X} pcbY={pinY(3)} radius={0.6} strokeWidth={0.12} />
        <silkscreentext pcbX={0} pcbY={pinY(3) - 2} text="28-pin\nROM" fontSize={1.0} />
      </footprint>
    </chip>
  );
};
