import Atari7800EdgeConnector, { ATARI_7800_CONNECTOR_OUTLINE } from "./Atari7800EdgeConnector";
import { ROM_27C256 } from "./ROM_27C256";
import { ATF16V8B } from "./ATF16V8B";
import { Latch74HCT373 } from "./74HCT373";
import { YM2149 } from "./YM2149";
import { LM358 } from "./LM358";

export default () => (
  <board
    routingDisabled
    outline={[
      { x: "-32mm", y: "35mm" },    // Top-left
      { x: "32mm", y: "35mm" },     // Top-right
      { x: "32mm", y: "-10mm" },    // Transition-right (Wide part)
      ...ATARI_7800_CONNECTOR_OUTLINE,
      { x: "-32mm", y: "-10mm" },   // Transition-left (Wide part)
    ]}
  >
    {/* Explicit Nets */}
    <net name="VCC" />
    <net name="GND" />
    <net name="ANALOG_A" />
    <net name="ANALOG_B" />
    <net name="ANALOG_C" />
    <net name="AUDIO_FINAL" />
    <net name="OPAMP_OUT" />
    <net name="RESET_DELAYED" />

    {/* Ground Plane & Basic Net Configuration */}
    <copperpour layer="bottom" connectsTo="net.GND" />
    <copperpour layer="top" connectsTo="net.GND" />

    {/* Dedicated Power Traces for stability (16 mil) */}
    <trace from=".J1 > .VCC" to=".U4 > .VCC" thickness="0.4mm" />
    <trace from=".J1 > .GND" to=".U4 > .GND" thickness="0.4mm" />
    <trace from=".U4 > .VCC" to=".U1 > .VCC" thickness="0.4mm" />
    <trace from=".U1 > .VCC" to=".U2 > .VCC" thickness="0.4mm" />
    <trace from=".U2 > .VCC" to=".U3 > .VCC" thickness="0.4mm" />
    <trace from=".U3 > .VCC" to=".U5 > .VCC" thickness="0.4mm" />

    {/* General Signal Width (6 mil baseline for pad escape) */}
    <trace from=".J1 > .A0" to=".U1 > .A0" thickness="0.15mm" />
    <trace from=".J1 > .D0" to=".U3 > .D0" thickness="0.15mm" />

    {/* --- Components --- */}

    {/* Atari 7800 Edge Connector */}
    <Atari7800EdgeConnector
      name="J1"
      pcbX="0mm" pcbY="-30.24mm"
      schX={-4} schY={0}
      connections={{
        VCC: "net.VCC",
        GND: "net.GND",
        "30": "net.GND",
        A0: "net.A0", A1: "net.A1", A2: "net.A2", A3: "net.A3", A4: "net.A4",
        A5: "net.A5", A6: "net.A6", A7: "net.A7", A8: "net.A8", A9: "net.A9",
        A10: "net.A10", A11: "net.A11", A12: "net.A12", A13: "net.A13", A14: "net.A14", A15: "net.A15",
        D0: "net.D0", D1: "net.D1", D2: "net.D2", D3: "net.D3", D4: "net.D4",
        D5: "net.D5", D6: "net.D6", D7: "net.D7",
        RW: "net.RW",
        HALT: "net.HALT",
        PHI2: "net.PHI2",
        Exaudio: "net.AUDIO_FINAL",
      }}
    />

    <group name="ROM_Group" pcbX="0mm" pcbY="-10mm" pcbRotation={270}>
      <ROM_27C256
        name="U1"
        schX={8} schY={4}
        connections={{
          VCC: "net.VCC",
          GND: "net.GND",
          OE: "net.GND",
          CE: "net.ROM_CE",
          A0: "net.A0", A1: "net.A1", A2: "net.A2", A3: "net.A3", A4: "net.A4",
          A5: "net.A5", A6: "net.A6", A7: "net.A7", A8: "net.A8", A9: "net.A9",
          A10: "net.A10", A11: "net.A11", A12: "net.A12", A13: "net.A13", A14: "net.A14",
          D0: "net.D0", D1: "net.D1", D2: "net.D2", D3: "net.D3", D4: "net.D4",
          D5: "net.D5", D6: "net.D6", D7: "net.D7",
        }}
      />
      <capacitor
        name="C1"
        capacitance="0.1uF"
        footprint="axial"
        pcbX="0mm" pcbY="21mm"
        schX={4} schY={6}
        connections={{ pin1: "net.VCC", pin2: "net.GND" }}
      />
    </group>

    <group name="Logic_Group" pcbX="0mm" pcbY="8mm" pcbRotation={270}>
      <group name="GAL_Group" pcbX="0mm" pcbY="-14mm">
        <ATF16V8B
          name="U2"
          pcbX="0mm" pcbY="0mm"
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
          pcbX="0mm" pcbY="14.2mm"
          schX={3} schY={6}
          connections={{ pin1: "net.VCC", pin2: "net.GND" }}
        />
      </group>

      <group name="Latch_Group" pcbX="0mm" pcbY="14mm">
        <Latch74HCT373
          name="U3"
          pcbX="0mm" pcbY="0mm"
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
          pcbX="0mm" pcbY="14.2mm"
          schX={4} schY={-6}
          connections={{ pin1: "net.VCC", pin2: "net.GND" }}
        />
      </group>
    </group>

    {/* --- Sound System (PSG and Analog Output) --- */}
    <group name="Sound_System_Group" pcbX="-4mm" pcbY="25mm">
      <group name="PSG_Group" pcbX="0mm" pcbY="0mm" pcbRotation={270}>
        <YM2149
          name="U4"
          schX={8} schY={-4}
          connections={{
            VCC: "net.VCC",
            BC2: "net.VCC",
            GND: "net.GND",
            DA0: "net.DA0", DA1: "net.DA1", DA2: "net.DA2", DA3: "net.DA3",
            DA4: "net.DA4", DA5: "net.DA5", DA6: "net.DA6", DA7: "net.DA7",
            CLK: "net.PHI2OUT",
            BDIR: "net.BDIR",
            BC1: "net.BC1",
            RESET: "net.RESET_DELAYED",
            A8: "net.VCC",
            A9: "net.GND",
            ANALOG_A: "net.ANALOG_A",
            ANALOG_B: "net.ANALOG_B",
            ANALOG_C: "net.ANALOG_C",
          }}
        />
        {/* RC Reset Circuit */}
        <resistor name="R_RESET" resistance="4.7k" footprint="axial" connections={{ pin1: "net.VCC", pin2: "net.RESET_DELAYED" }} />
        <capacitor name="C_RESET" capacitance="10uF" footprint="axial" polarized connections={{ pin1: "net.RESET_DELAYED", pin2: "net.GND" }} />
        <capacitor name="C4" capacitance="0.1uF" footprint="axial" connections={{ pin1: "net.VCC", pin2: "net.GND" }} />
      </group>

      {/* --- Audio Out Section (LM358 "Lokey" Active Shunt) --- */}
      <group name="Audio_Out_Group" pcbX="32mm" pcbY="0mm" pcbRotation={0}>
        {/* Passive Mixing directly onto the final bus */}
        <resistor name="R1" resistance="1k" footprint="axial" pcbX="-4mm" pcbY="-6mm" connections={{ pin1: "net.ANALOG_A", pin2: "net.AUDIO_FINAL" }} />
        <resistor name="R2" resistance="1k" footprint="axial" pcbX="-4mm" pcbY="-3mm" connections={{ pin1: "net.ANALOG_B", pin2: "net.AUDIO_FINAL" }} />
        <resistor name="R3" resistance="1k" footprint="axial" pcbX="-4mm" pcbY="0mm" connections={{ pin1: "net.ANALOG_C", pin2: "net.AUDIO_FINAL" }} />
        
        {/* LM358 Active Shunt Stage */}
        <LM358
           name="U5"
           pcbX="10mm" pcbY="0mm"
           connections={{
             VCC: "net.VCC",
             GND: "net.GND",
             IN1_POS: "net.GND",       // Pin 3: Grounded
             IN1_NEG: "net.OPAMP_OUT",  // Pin 2: Feedback point
             OUT1: "net.OPAMP_OUT",     // Pin 1: Shorted to Pin 2
           }}
           />
           <capacitor
           name="C6"
           capacitance="0.1uF"
           footprint="axial"
           pcbX="10mm" pcbY="14.2mm"
           schX={26} schY={-2}
           connections={{ pin1: "net.VCC", pin2: "net.GND" }}
           />
        {/* The "Grit" Resistor - Pull-down for the feedback node */}
        <resistor name="R_GRIT" resistance="4.7k" footprint="axial" pcbX="18mm" pcbY="-3mm" connections={{ pin1: "net.OPAMP_OUT", pin2: "net.GND" }} />

        {/* The "Coupling" Resistor - In series with the cap */}
        <resistor name="R_COUPLE" resistance="4.7k" footprint="axial" pcbX="18mm" pcbY="3mm" connections={{ pin1: "net.OPAMP_OUT", pin2: "net.CAP_PLUS" }} />

        {/* Coupling Capacitor (Active Shunt bridge to Final Bus) */}
        <capacitor
          name="C5"
          capacitance="10uF"
          footprint="axial"
          polarized
          pcbX="25mm" pcbY="4mm"
          connections={{
            pin1: "net.CAP_PLUS",     // Positive (+) connects to Resistor
            pin2: "net.AUDIO_FINAL",  // Negative (-) connects to Final Mix / Pin 18
          }}
        />
      </group>
    </group>

    {/* Board Labels */}
    <silkscreentext text="7800 YM" pcbX="25mm" pcbY="10mm" fontSize="3mm" />
    <silkscreentext text="v1.0" pcbX="25mm" pcbY="5mm" fontSize="1.5mm" />
  </board>
);
