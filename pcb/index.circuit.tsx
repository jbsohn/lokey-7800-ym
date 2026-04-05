import { Atari7800EdgeConnector } from "./Atari7800EdgeConnector";
import { ROM_27C256 } from "./ROM_27C256";
import { GAL16V8 } from "./GAL16V8";
import { Latch74HCT373 } from "./74HCT373";
import { YM2149 } from "./YM2149";

export default () => (
  <board
    outline={[
      { x: -26.65, y: 36.25 },    // Top-left
      { x: 26.65, y: 36.25 },     // Top-right
      { x: 26.65, y: -33.75 },    // Bottom-right
      { x: 18.05, y: -33.75 }, // Right edge of right notch
      { x: 18.05, y: -27 },    // Top-right of right notch
      { x: 14.97, y: -27 },    // Top-left of right notch
      { x: 14.97, y: -33.75 }, // Left edge of right notch
      { x: -14.97, y: -33.75 },// Right edge of left notch
      { x: -14.97, y: -27 },   // Top-right of left notch
      { x: -18.05, y: -27 },   // Top-left of left notch
      { x: -18.05, y: -33.75 },// Left edge of left notch
      { x: -26.65, y: -33.75 },// Bottom-left
    ]}
  >
    {/* Explicit Nets */}
    <net name="VCC" />
    <net name="GND" />
    <net name="ANALOG_A" />
    <net name="ANALOG_B" />
    <net name="ANALOG_C" />
    <net name="AUDIO_MIXED" />

    {/* Ground Plane & Basic Net Configuration */}
    <copperpour layer="bottom" connectsTo="net.GND" />
    <copperpour layer="top" connectsTo="net.GND" />

    {/* Dedicated Power Traces for stability (16 mil) */}
    <trace from=".J1 > .VCC" to=".U4 > .VCC" thickness={0.4} />
    <trace from=".J1 > .GND" to=".U4 > .VSS" thickness={0.4} />
    <trace from=".U4 > .VCC" to=".U1 > .VCC" thickness={0.4} />
    <trace from=".U1 > .VCC" to=".U2 > .VCC" thickness={0.4} />
    <trace from=".U2 > .VCC" to=".U3 > .VCC" thickness={0.4} />

    {/* General Signal Width (6 mil baseline for pad escape) */}
    <trace from=".J1 > .A0" to=".U1 > .A0" thickness={0.15} />
    <trace from=".J1 > .D0" to=".U3 > .D0" thickness={0.15} />

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
        Exaudio: "net.AUDIO_MIXED",
      }}
    />

    {/* --- Logic Group (Centered between Top and Connector) --- */}
    <group name="Logic_Group" pcbX={0} pcbY={4.755}>
      {/* Decoding & Latches - Horizontal Brain */}
      <group name="U2_Group" pcbX={0} pcbY={15}>
        <GAL16V8
          name="U2"
          pcbX={0} pcbY={0}
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
        <capacitor
          name="C2"
          capacitance="0.1uF"
          footprint="axial"
          pcbX={0} pcbY={-14.2}
          pcbRotation={0}
          schX={3} schY={6}
          connections={{ pin1: "net.VCC", pin2: "net.GND" }}
        />
      </group>
  
      <group name="U3_Group" pcbX={0} pcbY={-15}>
        <Latch74HCT373
          name="U3"
          pcbX={0} pcbY={0}
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
        <capacitor
          name="C3"
          capacitance="0.1uF"
          footprint="axial"
          pcbX={0} pcbY={-14.2}
          pcbRotation={0}
          schX={4} schY={-6}
          connections={{ pin1: "net.VCC", pin2: "net.GND" }}
        />
      </group>
    </group>

    {/* Memory - Left Column (Shifted down for edge clearance) */}
    <group name="ROM_Group" pcbX={-14} pcbY={5}>
      <ROM_27C256
        name="U1"
        pcbRotation={180}
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
      <capacitor
        name="C1"
        capacitance="0.1uF"
        footprint="axial"
        pcbX={0} pcbY={-21.0}
        pcbRotation={0}
        schX={4} schY={6}
        connections={{ pin1: "net.VCC", pin2: "net.GND" }}
      />
    </group>

    {/* Sound - Right Column (Shifted down for edge clearance) */}
    <group name="PSG_Group" pcbX={14} pcbY={5}>
      <YM2149
        name="U4"
        pcbRotation={180}
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
          ANALOG_A: "net.ANALOG_A",
          ANALOG_B: "net.ANALOG_B",
          ANALOG_C: "net.ANALOG_C",
        }}
      />
      <capacitor
        name="C4"
        capacitance="0.1uF"
        footprint="axial"
        pcbX={0} pcbY={-28.25}
        pcbRotation={0}
        schX={3} schY={-6}
        connections={{ pin1: "net.VCC", pin2: "net.GND" }}
      />
    </group>

    {/* --- Audio Section (Aligned on the left edge above ROM) --- */}
    <group name="Audio_Group" pcbX={-17} pcbY={28}>
      <resistor name="R1" resistance="1k" footprint="axial" pcbX={-6} pcbY={0} pcbRotation={90} schX={13} schY={-3} connections={{ pin1: "net.ANALOG_A", pin2: "net.AUDIO_MIXED" }} />
      <resistor name="R2" resistance="1k" footprint="axial" pcbX={-4} pcbY={0} pcbRotation={90} schX={13} schY={-4} connections={{ pin1: "net.ANALOG_B", pin2: "net.AUDIO_MIXED" }} />
      <resistor name="R3" resistance="1k" footprint="axial" pcbX={-2} pcbY={0} pcbRotation={90} schX={13} schY={-5} connections={{ pin1: "net.ANALOG_C", pin2: "net.AUDIO_MIXED" }} />
      <capacitor name="C5" capacitance="10uF" footprint="axial" polarized pcbX={2} pcbY={0} pcbRotation={90} schX={16} schY={-4} connections={{ pin1: "net.AUDIO_MIXED", pin2: ".J1 > .Exaudio" }} />
    </group>

    {/* Board Labels */}
    <silkscreentext text="7800 YM" pcbX={6} pcbY={-25} fontSize={3} />
    <silkscreentext text="v1.0" pcbX={16} pcbY={-25} fontSize={1.5} />
  </board>
);
