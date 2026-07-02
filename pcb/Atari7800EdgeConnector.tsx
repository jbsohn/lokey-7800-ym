import { type ChipProps } from "tscircuit";

export const ATARI_7800_CONNECTOR_OUTLINE = [
  // Connector notch geometry only — step-in handled by shoulder recesses in main outline
  { x: "23.5mm", y: "-40mm" },         // Bottom-right
  { x: "17.71mm", y: "-40mm" },        // Right notch outer
  { x: "17.71mm", y: "-30.3mm" },      // Right notch top-outer
  { x: "15.31mm", y: "-30.3mm" },      // Right notch top-inner
  { x: "15.31mm", y: "-40mm" },        // Right notch inner
  { x: "-15.31mm", y: "-40mm" },       // Left notch inner
  { x: "-15.31mm", y: "-30.3mm" },     // Left notch top-inner
  { x: "-17.71mm", y: "-30.3mm" },     // Left notch top-outer
  { x: "-17.71mm", y: "-40mm" },       // Left notch outer
  { x: "-23.5mm", y: "-40mm" },        // Bottom-left
];

const Atari7800EdgeConnector = (props: ChipProps) => {
  const padWidth = 2;
  const padHeight = 7;
  
  // Front side pads (1-16)
  const frontPads = [
    { num: 1, x: -21.59 }, { num: 2, x: -19.05 }, { num: 3, x: -13.97 }, { num: 4, x: -11.43 },
    { num: 5, x: -8.89 }, { num: 6, x: -6.35 }, { num: 7, x: -3.81 }, { num: 8, x: -1.27 },
    { num: 9, x: 1.27 }, { num: 10, x: 3.81 }, { num: 11, x: 6.35 }, { num: 12, x: 8.89 },
    { num: 13, x: 11.43 }, { num: 14, x: 13.97 }, { num: 15, x: 19.05 }, { num: 16, x: 21.59 }
  ];

  // Back side pads (17-32)
  const backPads = [
    { num: 17, x: 21.59 }, { num: 18, x: 19.05 }, { num: 19, x: 13.97 }, { num: 20, x: 11.43 },
    { num: 21, x: 8.89 }, { num: 22, x: 6.35 }, { num: 23, x: 3.81 }, { num: 24, x: 1.27 },
    { num: 25, x: -1.27 }, { num: 26, x: -3.81 }, { num: 27, x: -6.35 }, { num: 28, x: -8.89 },
    { num: 29, x: -11.43 }, { num: 30, x: -13.97 }, { num: 31, x: -19.05 }, { num: 32, x: -21.59 }
  ];

  return (
    <chip
      {...props}
      allowOffBoard
      pinLabels={{
        1: "RW", 2: "HALT", 3: "D3", 4: "D4", 5: "D5", 6: "D6", 7: "D7", 8: "A12",
        9: "A10", 10: "A11", 11: "A9", 12: "A8", 13: "VCC", 14: "GND", 15: "A13", 16: "A14",
        17: "A15", 18: "Exaudio", 19: "A7", 20: "A6", 21: "A5", 22: "A4", 23: "A3", 24: "A2",
        25: "A1", 26: "A0", 27: "D0", 28: "D1", 29: "D2", 30: "GND", 31: "IRQ", 32: "PHI2"
      }}
      schPinArrangement={{
        topSide: { pins: ["VCC"], direction: "left-to-right" },
        bottomSide: { pins: ["GND"], direction: "left-to-right" },
        rightSide: {
          pins: [
            "A15", "A14", "A13", "A12", "A11", "A10", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "A1", "A0",
            "D7", "D6", "D5", "D4", "D3", "D2", "D1", "D0",
            "PHI2", "RW", "HALT", "IRQ", "Exaudio"
          ],
          direction: "top-to-bottom"
        }
      }}
    >
      <footprint>
        {frontPads.map(pad => (
          <smtpad
            key={`front-${pad.num}`}
            shape="rect"
            width={padWidth}
            height={padHeight}
            pcbX={pad.x}
            pcbY={0}
            layer="top"
            portHints={[`pin${pad.num}`]}
          />
        ))}
        {backPads.map(pad => (
          <smtpad
            key={`back-${pad.num}`}
            shape="rect"
            width={padWidth}
            height={padHeight}
            pcbX={pad.x}
            pcbY={0}
            layer="bottom"
            portHints={[`pin${pad.num}`]}
          />
        ))}
        <silkscreentext pcbX={-23} pcbY={-5} text="1" fontSize={1.2} />
        <silkscreentext pcbX={23} pcbY={-5} text="16" fontSize={1.2} />
      </footprint>
    </chip>
  );
};

export default Atari7800EdgeConnector;
