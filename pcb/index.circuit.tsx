import Atari7800EdgeConnector, { ATARI_7800_CONNECTOR_OUTLINE } from "./Atari7800EdgeConnector";
import { ROM_27C256 } from "./ROM_27C256";
import { ATF16V8B } from "./ATF16V8B";
import { Latch74HCT373 } from "./74HCT373";
import { YM2149 } from "./YM2149";
import { LM358 } from "./LM358";

export default () => (
  <board
    outline={[
      { x: "-32mm", y: "40mm" },      // Top-left
      { x: "32mm", y: "40mm" },       // Top-right
      { x: "32mm", y: "-15.75mm" },   // Chamfer start — right shoulder corner
      { x: "31.5mm", y: "-16.25mm" },  // Chamfer end
      ...ATARI_7800_CONNECTOR_OUTLINE,
      { x: "-31.5mm", y: "-16.25mm" }, // Chamfer end — left shoulder corner
      { x: "-32mm", y: "-15.75mm" },  // Chamfer start
    ]}
    routingDisabled={true}
  >
    {/* Explicit Nets */}
    <net name="VCC" />
    <net name="GND" />
    <net name="ANALOG_A" />
    <net name="ANALOG_B" />
    <net name="ANALOG_C" />
    <net name="SUM_NODE" />
    <net name="CAP_PLUS" />
    <net name="OPAMP_FB" />
    <net name="RESET_DELAYED" />
    <net name="U5_UNUSED_FB" />

    {/* Ground Plane & Basic Net Configuration */}
    <copperpour layer="bottom" connectsTo="net.GND" />
    <copperpour layer="top" connectsTo="net.GND" />

    {/* Stitch via to ensure GND zone continuity near right shoulder */}
    <via pcbX="30mm" pcbY="-14.25mm" connectsTo="net.GND" />

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

    {/* Critical edge-routed signals — keep away from connector notches */}
    <trace from=".J1 > .A13" to=".U1 > .A13" thickness="0.15mm" />
    <trace from=".J1 > .A14" to=".U1 > .A14" thickness="0.15mm" />
    <trace from=".J1 > .HALT" to=".U2 > .HALT" thickness="0.15mm" />

    {/* --- Components --- */}

    {/* Atari 7800 Edge Connector */}
    <Atari7800EdgeConnector
      name="J1"
      pcbX="0mm" pcbY="-36.49mm"
      schX={-12} schY={0}
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
        Exaudio: "net.SUM_NODE",
      }}
    />

    <group
      name="Rom"
      pcbX="0mm" pcbY="-18.25mm">
      <ROM_27C256
        name="U1"
        schX={2} schY={-8}
        pcbRotation={270}
        connections={{
          VCC: "net.VCC",
          VPP: "net.VCC",
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
        pcbX="20mm" pcbY="0mm"
        schX={4} schY={-12}
        pcbRotation={270}
        connections={{ pin1: "net.VCC", pin2: "net.GND" }}
      />
    </group>

    <group name="GAL"
      pcbX="-16mm" pcbY="-2.25mm">
      <ATF16V8B
        name="U2"
        schX={-2} schY={4}
        pcbRotation={270}
        connections={{
          VCC: "net.VCC",
          GND: "net.GND",
          A15: "net.A15",
          A14: "net.A14",
          A0: "net.A0",
          HALT: "net.HALT",
          RW: "net.RW",
          PHI2: "net.PHI2",
          ROM_CE: "net.ROM_CE",
          BDIR: "net.BDIR",
          BC1: "net.BC1",
          PHI2OUT: "net.PHI2OUT",
          YM_LE: "net.YM_LE",
        }}
      />
      <capacitor
        name="C2"
        capacitance="0.1uF"
        footprint="axial"
        schX={0} schY={6}
        pcbX="15mm" pcbY="0mm"
        pcbRotation={270}
        connections={{ pin1: "net.VCC", pin2: "net.GND" }}
      />
    </group>

    <group name="Latch"
      pcbX="14mm" pcbY="-2.25mm">
      <Latch74HCT373
        name="U3"
        schX={6} schY={4}
        pcbRotation={270}
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
        schX={8} schY={8}
        pcbX="15mm" pcbY="0mm"
        pcbRotation={270}
        connections={{ pin1: "net.VCC", pin2: "net.GND" }}
      />
    </group>

    <group
      name="YM"
      pcbX="0mm" pcbY="15.75mm">
      <YM2149
        name="U4"
        schX={16} schY={0}
        pcbRotation={270}
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
      <capacitor name="C4"
        capacitance="0.1uF"
        footprint="axial"
        schX={18} schY={5}
        pcbX="28mm" pcbY="0mm"
        pcbRotation={270}
        connections={{
          pin1: "net.VCC", pin2: "net.GND"
        }} />
      <resistor name="RRESET1" resistance="4.7k" footprint="axial"
        schX={12} schY={10}
        pcbX="-14mm" pcbY="-11.5mm"
        connections={{ pin1: "net.VCC", pin2: "net.RESET_DELAYED" }} />
      <capacitor
        name="CRESET1" capacitance="10uF" footprint="axial" polarized schX={14} schY={7}
        pcbX="-20mm" pcbY="-11.5mm"
        connections={{
          pin1: "net.RESET_DELAYED", pin2: "net.GND"
        }} />
      <resistor name="R1" resistance="1k" footprint="axial"
        pcbX="22mm" pcbY="10.5mm"
        schX={24} schY={5}
        connections={{
          pin1: "net.ANALOG_A", pin2: "net.SUM_NODE"
        }} />
      <resistor name="R2" resistance="1k" footprint="axial"
        pcbX="16mm" pcbY="10.5mm"
        schX={24} schY={2} connections={{
          pin1: "net.ANALOG_B", pin2: "net.SUM_NODE"
        }} />
      <resistor name="R3" resistance="1k" footprint="axial"
        pcbX="20mm" pcbY="-11.5mm"
        schX={24} schY={-1}
        connections={{
          pin1: "net.ANALOG_C", pin2: "net.SUM_NODE"
        }} />
    </group>


    <group name="Amp"
      pcbY="33.75mm" pcbX="0mm">
      <LM358
        name="U5"
        schX={28} schY={2}
        pcbRotation={270}
        connections={{
          VCC: "net.VCC",
          GND: "net.GND",
          IN1_POS: "net.GND",        // Pin 3: Tied to Ground
          IN1_NEG: "net.OPAMP_FB",   // Pin 2: Feedback node (shorted to Pin 1)
          OUT1: "net.OPAMP_FB",      // Pin 1: Shorted to Pin 2 (Feedback node)
          IN2_POS: "net.GND",        // Pin 5: Unused section - input tied to GND
          IN2_NEG: "net.U5_UNUSED_FB", // Pin 6: Unused section - shorted to output
          OUT2: "net.U5_UNUSED_FB",  // Pin 7: Unity-gain follower (output = GND)
        }}
      />
      <resistor name="RGRIT1" resistance="4.7k" footprint="axial" pcbX="18mm" pcbY="-3mm" schX={34} schY={6} connections={{ pin1: "net.OPAMP_FB", pin2: "net.GND" }} />
      <resistor name="RGRIT2" resistance="4.7k" footprint="axial" pcbX="18mm" pcbY="0mm" schX={34} schY={4} connections={{ pin1: "net.OPAMP_FB", pin2: "net.GND" }} />
      <resistor name="RFEEDBACK1" resistance="4.7k" footprint="axial" pcbX="18mm" pcbY="3mm" schX={34} schY={2} connections={{ pin1: "net.OPAMP_FB", pin2: "net.CAP_PLUS" }} />
      <capacitor
        name="C5"
        capacitance="10uF"
        footprint="axial"
        polarized
        pcbX="25mm" pcbY="4mm"
        schX={40} schY={2}
        connections={{
          pin1: "net.CAP_PLUS",     // Positive (+) to Resistor
          pin2: "net.SUM_NODE",     // Negative (-) to Sum Node
        }}
      />
      <capacitor
        name="C6"
        capacitance="0.1uF"
        footprint="axial"
        pcbX="8mm" pcbY="0mm"
        pcbRotation={270}
        schX={32} schY={-4}
        connections={{ pin1: "net.VCC", pin2: "net.GND" }}
      />
    </group>

    <silkscreentext
      text="Lokey 7800 YM v0.1"
      pcbX="0mm" pcbY="-30.25mm"
      fontSize="2mm" />
  </board>
);
