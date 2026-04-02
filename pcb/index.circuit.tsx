import { Atari7800EdgeConnector } from "./Atari7800EdgeConnector";
import { ROM_27C256 } from "./ROM_27C256";
import { GAL16V8 } from "./GAL16V8";
import { Latch74HCT373 } from "./74HCT373";
import { YM2149 } from "./YM2149";

export default () => (
  <board
    outline={[
      { x: -24, y: 33.75 },    // Top-left
      { x: 24, y: 33.75 },     // Top-right
      { x: 24, y: -33.75 },    // Bottom-right
      { x: 18.05, y: -33.75 }, // Right edge of right notch
      { x: 18.05, y: -27 },    // Top-right of right notch
      { x: 14.97, y: -27 },    // Top-left of right notch
      { x: 14.97, y: -33.75 }, // Left edge of right notch
      { x: -14.97, y: -33.75 },// Right edge of left notch
      { x: -14.97, y: -27 },   // Top-right of left notch
      { x: -18.05, y: -27 },   // Top-left of left notch
      { x: -18.05, y: -33.75 },// Left edge of left notch
      { x: -24, y: -33.75 },   // Bottom-left
    ]}
    routingDisabled
  >
    {/* Explicit Nets */}
    <net name="VCC" />
    <net name="GND" />

    {/* Ground Plane for easier routing */}
    <copperpour layer="bottom" connectsTo="net.GND" />
    <copperpour layer="top" connectsTo="net.GND" />

    {/* --- Components --- */}

    {/* Atari 7800 Edge Connector - Aligned flush to bottom center */}
    <Atari7800EdgeConnector
      name="J1"
      pcbX={-0.02} pcbY={-30.24}
      schX={-4} schY={0}
      connections={{
        VCC: "net.VCC",
        GND: "net.GND",
        "30": "net.GND",
        A0: "net.A0",
        A1: "net.A1",
        A2: "net.A2",
        A3: "net.A3",
        A4: "net.A4",
        A5: "net.A5",
        A6: "net.A6",
        A7: "net.A7",
        A8: "net.A8",
        A9: "net.A9",
        A10: "net.A10",
        A11: "net.A11",
        A12: "net.A12",
        A13: "net.A13",
        A14: "net.A14",
        A15: "net.A15",
        D0: "net.D0",
        D1: "net.D1",
        D2: "net.D2",
        D3: "net.D3",
        D4: "net.D4",
        D5: "net.D5",
        D6: "net.D6",
        D7: "net.D7",
        RW: "net.RW",
        HALT: "net.HALT",
        PHI2: "net.PHI2",
      }}
    />

    {/* Decoding & Latches - Left Column (Shifted for 0.5mm clearance) */}
    <GAL16V8
      name="U2"
      pcbX={-18.5} pcbY={18.05}
      schX={2} schY={4}
      connections={{
        VCC: "net.VCC",
        GND: "net.GND",
        I1: "net.A15",
        I2: "net.A14",
        I3: "net.A0",
        I4: "net.HALT",
        I5: "net.RW",
        I6: "net.PHI2",
        O1: "net.ROM_CE",
        O2: "net.BDIR",
        O3: "net.BC1",
        O4: "net.PHI2OUT",
        O5: "net.YM_LE",
      }}
    />

    <Latch74HCT373
      name="U3"
      pcbX={-18.5} pcbY={-12.75}
      schX={2} schY={-4}
      connections={{
        VCC: "net.VCC",
        GND: "net.GND",
        OE: "net.GND",
        LE: "net.YM_LE",
        D0: "net.D0",
        D1: "net.D1",
        D2: "net.D2",
        D3: "net.D3",
        D4: "net.D4",
        D5: "net.D5",
        D6: "net.D6",
        D7: "net.D7",
        Q0: "net.DA0",
        Q1: "net.DA1",
        Q2: "net.DA2",
        Q3: "net.DA3",
        Q4: "net.DA4",
        Q5: "net.DA5",
        Q6: "net.DA6",
        Q7: "net.DA7",
      }}
    />

    {/* Memory - Middle Column (Shifted for 0.5mm clearance) */}
    <ROM_27C256
      name="U1"
      pcbX={-4} pcbY={-7.5}
      schX={8} schY={4}
      connections={{
        VCC: "net.VCC",
        VPP: "net.VCC",
        GND: "net.GND",
        OE: "net.GND",
        CE: "net.ROM_CE",
        A0: "net.A0",
        A1: "net.A1",
        A2: "net.A2",
        A3: "net.A3",
        A4: "net.A4",
        A5: "net.A5",
        A6: "net.A6",
        A7: "net.A7",
        A8: "net.A8",
        A9: "net.A9",
        A10: "net.A10",
        A11: "net.A11",
        A12: "net.A12",
        A13: "net.A13",
        A14: "net.A14",
        D0: "net.D0",
        D1: "net.D1",
        D2: "net.D2",
        D3: "net.D3",
        D4: "net.D4",
        D5: "net.D5",
        D6: "net.D6",
        D7: "net.D7",
      }}
    />

    {/* Sound - Right Column (Shifted for 0.5mm clearance) */}
    <YM2149
      name="U4"
      pcbX={14.5} pcbY={4}
      schX={8} schY={-4}
      connections={{
        VCC: "net.VCC",
        VCC_ANALOG: "net.VCC",
        BC2: "net.VCC",
        VSS: "net.GND",
        ANALOG_C_REF: "net.GND",
        DA0: "net.DA0",
        DA1: "net.DA1",
        DA2: "net.DA2",
        DA3: "net.DA3",
        DA4: "net.DA4",
        DA5: "net.DA5",
        DA6: "net.DA6",
        DA7: "net.DA7",
        CLK: "net.PHI2OUT",
        BDIR: "net.BDIR",
        BC1: "net.BC1",
      }}
    />

    {/* --- Passives (Through-hole Axial on FRONT Side) --- */}
    
    {/* Decoupling Capacitors */}
    <capacitor name="C2" capacitance="0.1uF" footprint="axial" pcbX={-18.5} pcbY={32.25} pcbRotation={0} schX={3} schY={6} connections={{ pin1: "net.VCC", pin2: "net.GND" }} />
    <capacitor name="C1" capacitance="0.1uF" footprint="axial" pcbX={-4} pcbY={13.5} pcbRotation={0} schX={4} schY={6} connections={{ pin1: "net.VCC", pin2: "net.GND" }} />
    
    {/* Alley Decoupling Capacitors */}
    <capacitor name="C3" capacitance="0.1uF" footprint="axial" pcbX={-18.5} pcbY={1.5} pcbRotation={0} schX={4} schY={-6} connections={{ pin1: "net.VCC", pin2: "net.GND" }} />
    <capacitor name="C4" capacitance="0.1uF" footprint="axial" pcbX={14.5} pcbY={32.25} pcbRotation={0} schX={3} schY={-6} connections={{ pin1: "net.VCC", pin2: "net.GND" }} />

    {/* Audio Section - Center-aligned with U1 */}
    <capacitor name="C5" capacitance="10uF" footprint="axial" polarized pcbX={-4} pcbY={32.25} pcbRotation={0} schX={16} schY={-4} connections={{ pin1: "net.AUDIO_MIXED", pin2: ".J1 > .Exaudio" }} />
    <resistor name="R3" resistance="1k" footprint="axial" pcbX={-4} pcbY={29.25} pcbRotation={0} schX={13} schY={-5} connections={{ pin1: ".U4 > .ANALOG_C", pin2: "net.AUDIO_MIXED" }} />
    <resistor name="R2" resistance="1k" footprint="axial" pcbX={-4} pcbY={26.25} pcbRotation={0} schX={13} schY={-4} connections={{ pin1: ".U4 > .ANALOG_B", pin2: "net.AUDIO_MIXED" }} />
    <resistor name="R1" resistance="1k" footprint="axial" pcbX={-4} pcbY={23.25} pcbRotation={0} schX={13} schY={-3} connections={{ pin1: ".U4 > .ANALOG_A", pin2: "net.AUDIO_MIXED" }} />

    {/* Board Labels */}
    <silkscreentext text="7800 YM" pcbX={6} pcbY={-25} fontSize={3} />
    <silkscreentext text="v1.0" pcbX={16} pcbY={-25} fontSize={1.5} />

    {/* Manual Audio Traces */}
    <trace from=".U4 > .ANALOG_A" to=".R1 > .pin1" />
    <trace from=".U4 > .ANALOG_B" to=".R2 > .pin1" />
    <trace from=".U4 > .ANALOG_C" to=".R3 > .pin1" />
    <trace from=".C5 > .pin2" to=".J1 > .Exaudio" />
  </board>
);
