import Atari7800EdgeConnector, { ATARI_7800_CONNECTOR_OUTLINE } from "./Atari7800EdgeConnector";
import { ROM_27Cxxx } from "./ROM_27Cxxx";
import { ATF22V10 } from "./ATF22V10";
import { Latch74HCT373 } from "./74HCT373";
import { YM2149 } from "./YM2149";
import { LM358 } from "./LM358";

export default () => (
  <board
    outline={[
      { x: "-30mm", y: "40mm" },         // Top-left (±30mm upper body)
      { x: "30mm", y: "40mm" },          // Top-right
      { x: "30mm", y: "9.5mm" },         // Right, step in at shoulder top
      { x: "20.64mm", y: "9.5mm" },      // Right shoulder recess (2.86mm from ±23.5mm, matching cart case)
      { x: "20.64mm", y: "-0.63mm" },   // Right mid-notch top (rail grip slot)
      { x: "19.05mm", y: "-0.63mm" },
      { x: "19.05mm", y: "-5.08mm" },
      { x: "20.64mm", y: "-5.08mm" },   // Right mid-notch bottom
      { x: "20.64mm", y: "-7mm" },       // Right shoulder bottom
      { x: "23.5mm", y: "-7mm" },        // Right back to connector width
      ...ATARI_7800_CONNECTOR_OUTLINE,
      { x: "-23.5mm", y: "-7mm" },       // Left back to connector width
      { x: "-20.64mm", y: "-7mm" },      // Left shoulder bottom
      { x: "-20.64mm", y: "-5.08mm" },   // Left mid-notch bottom (rail grip slot)
      { x: "-19.05mm", y: "-5.08mm" },
      { x: "-19.05mm", y: "-0.63mm" },
      { x: "-20.64mm", y: "-0.63mm" },   // Left mid-notch top
      { x: "-20.64mm", y: "9.5mm" },     // Left shoulder top
      { x: "-30mm", y: "9.5mm" },        // Left step out to upper body
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
    <net name="OPAMP_OUT" />
    <net name="CAP_PLUS" />
    <net name="OPAMP_OUT_AC" />
    <net name="RESET_DELAYED" />
    <net name="AMP_UNUSED_FB" />
    <net name="ROM_A15" />   {/* socket pad 3:  A15 ← GAL ROM_A15 (IOA0) */}
    <net name="ROM_A16" />   {/* socket pad 2:  A16 ← GAL ROM_A16 (IOA1) */}
    <net name="ROM_A17" />   {/* socket pad 30: A17 ← GAL ROM_A17 (IOA2) */}
    <net name="ROM_A18" />   {/* socket pad 31: A18 ← GAL ROM_A18 (IOA3) */}
    <net name="YM_IOA0" />   {/* YM1 pin 21 (IOA0) → ROM A15 bank bit */}
    <net name="YM_IOA1" />   {/* YM1 pin 20 (IOA1) → ROM A16 bank bit */}
    <net name="YM_IOA2" />   {/* YM1 pin 19 (IOA2) → ROM A17 bank bit */}
    <net name="YM_IOA3" />   {/* YM1 pin 18 (IOA3) → ROM A18 bank bit */}

    {/* Ground Plane & Basic Net Configuration */}
    <copperpour
      layer="bottom"
      connectsTo="net.GND"
    />
    <copperpour
      layer="top"
      connectsTo="net.GND"
    />

    {/* Stitch via to ensure GND zone continuity near right shoulder */}
    <chip
      name="U6"
      pcbX="21mm"
      pcbY="-8.5mm"
      pinLabels={{ 1: "GND" }}
      connections={{ 1: "net.GND" }}
    >
      <footprint>
        <platedhole
          shape="circle"
          holeDiameter="0.3mm"
          outerDiameter="0.6mm"
          pcbX="0mm"
          pcbY="0mm"
          portHints={["pin1"]}
        />
      </footprint>
    </chip>

    <trace
      from=".U_LATCH > .GND"
      to=".U6 > .pin1"
      thickness="0.4mm"
    />

    {/* Stitch via to ensure GND zone continuity near right middle (next to YM) */}
    <chip
      name="U7"
      pcbX="26mm"
      pcbY="15.75mm"
      pinLabels={{ 1: "GND" }}
      connections={{ 1: "net.GND" }}
    >
      <footprint>
        <platedhole
          shape="circle"
          holeDiameter="0.3mm"
          outerDiameter="0.6mm"
          pcbX="0mm"
          pcbY="0mm"
          portHints={["pin1"]}
        />
      </footprint>
    </chip>

    <trace
      from=".C_YM > .pin2"
      to=".U7 > .pin1"
      thickness="0.4mm"
    />

    {/* Dedicated Power Traces for stability (16 mil) */}
    <trace
      from=".J1 > .VCC"
      to=".U_YM > .VCC"
      thickness="0.4mm"
    />
    <trace
      from=".J1 > .GND"
      to=".U_YM > .GND"
      thickness="0.4mm"
    />
    <trace
      from=".U_YM > .VCC"
      to=".U_ROM > .VCC"
      thickness="0.4mm"
    />
    <trace
      from=".U_ROM > .VCC"
      to=".U_GAL > .VCC"
      thickness="0.4mm"
    />
    <trace
      from=".U_GAL > .VCC"
      to=".U_LATCH > .VCC"
      thickness="0.4mm"
    />
    <trace
      from=".U_LATCH > .VCC"
      to=".U_AMP > .VCC"
      thickness="0.4mm"
    />

    {/* General Signal Width (6 mil baseline for pad escape) */}
    <trace
      from=".J1 > .A0"
      to=".U_ROM > .A0"
      thickness="0.15mm"
    />
    <trace
      from=".J1 > .D0"
      to=".U_LATCH > .D0"
      thickness="0.15mm"
    />

    {/* Critical edge-routed signals — keep away from connector notches */}
    <trace
      from=".J1 > .A13"
      to=".U_ROM > .A13"
      thickness="0.15mm"
    />
    <trace
      from=".J1 > .HALT"
      to=".U_GAL > .HALT"
      thickness="0.15mm"
    />

    {/* --- Components --- */}

    {/* Atari 7800 Edge Connector */}
    <Atari7800EdgeConnector
      name="J1"
      pcbX="0mm"
      pcbY="-36.49mm"
      schX={-12}
      schY={0}
      connections={{
        VCC: "net.VCC",
        GND: "net.GND",
        "30": "net.GND",
        A0: "net.A0", A1: "net.A1", A2: "net.A2", A3: "net.A3", A4: "net.A4",
        A5: "net.A5", A6: "net.A6", A7: "net.A7", A8: "net.A8", A9: "net.A9",
        A10: "net.A10", A11: "net.A11", A12: "net.A12", A13: "net.A13", A14: "net.A14",
        A15: "net.A15",
        D0: "net.D0", D1: "net.D1", D2: "net.D2", D3: "net.D3", D4: "net.D4",
        D5: "net.D5", D6: "net.D6", D7: "net.D7",
        RW: "net.RW",
        HALT: "net.HALT",
        PHI2: "net.PHI2",
        Exaudio: "net.OPAMP_OUT_AC",
      }}
    />

    <group
      name="Rom"
      pcbX="-1mm"
      pcbY="-20.5mm"
    >
      <ROM_27Cxxx
        name="U_ROM"
        schX={1}
        schY={-8}
        pcbRotation={270}
        connections={{
          VCC: "net.VCC",
          VPP: "net.VCC",        // pin 1: VCC (read mode)
          GND: "net.GND",
          OE: "net.GND",
          CE: "net.ROM_CE",
          A0: "net.A0", A1: "net.A1", A2: "net.A2", A3: "net.A3", A4: "net.A4",
          A5: "net.A5", A6: "net.A6", A7: "net.A7", A8: "net.A8", A9: "net.A9",
          A10: "net.A10", A11: "net.A11", A12: "net.A12", A13: "net.A13", A14: "net.A14",
          A15: "net.ROM_A15",    // pin 3:  bank bit 0, driven by PLD from YM_IOA0
          A16: "net.ROM_A16",    // pin 2:  bank bit 1, driven by PLD from YM_IOA1
          A17: "net.ROM_A17",    // pin 30: bank bit 2, driven by PLD from YM_IOA2 (256K+)
          A18: "net.ROM_A18",    // pin 31: bank bit 3, driven by PLD from YM_IOA3 (512K)
          D0: "net.D0", D1: "net.D1", D2: "net.D2", D3: "net.D3", D4: "net.D4",
          D5: "net.D5", D6: "net.D6", D7: "net.D7",
        }}
      />
      <capacitor
        name="C_ROM"
        capacitance="0.1uF"
        footprint="axial_p7.62mm"
        pcbX="22mm"
        pcbY="0mm"
        schX={4}
        schY={-12}
        pcbRotation={270}
        connections={{
          pin1: "net.VCC",
          pin2: "net.GND",
        }}
      />
      <group
        name="GAL"
        pcbX="0mm"
        pcbY="0mm"
      >
        <ATF22V10
          name="U_GAL"
          schX={-5}
          schY={0}
          pcbRotation={270}
          layer="bottom"
          connections={{
            VCC: "net.VCC",
            GND: "net.GND",
            HALT: "net.HALT",
            A15: "net.A15",
            A14: "net.A14",
            A13: "net.A13",
            A12: "net.A12",
            A11: "net.A11",
            A0: "net.A0",
            RW: "net.RW",
            PHI2: "net.PHI2",
            IOA0: "net.YM_IOA0",
            IOA1: "net.YM_IOA1",
            IOA2: "net.YM_IOA2",
            IOA3: "net.YM_IOA3",
            ROM_CE: "net.ROM_CE",
            BDIR: "net.BDIR",
            BC1: "net.BC1",
            PHI2OUT: "net.PHI2OUT",
            YM_LE: "net.YM_LE",
            ROM_A15: "net.ROM_A15",
            ROM_A16: "net.ROM_A16",
            ROM_A17: "net.ROM_A17",
            ROM_A18: "net.ROM_A18",
          }}
        />
        <capacitor
          name="C_GAL"
          capacitance="0.1uF"
          footprint="axial_p7.62mm"
          schX={-3}
          schY={4}
          pcbX="17mm"
          pcbY="0mm"
          pcbRotation={270}
          layer="bottom"
          connections={{
            pin1: "net.VCC",
            pin2: "net.GND"
          }}
        />
      </group>
    </group>


    <group
      name="Latch"
      pcbX="0mm"
      pcbY="24mm"
    >
      <Latch74HCT373
        name="U_LATCH"
        schX={6}
        schY={4}
        pcbRotation={270}
        layer="bottom"
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
        name="C_LATCH"
        capacitance="0.1uF"
        footprint="axial_p7.62mm"
        schX={8}
        schY={8}
        pcbX="15mm"
        pcbY="0mm"
        pcbRotation={270}
        layer="bottom"
        connections={{
          pin1: "net.VCC",
          pin2: "net.GND"
        }}
      />
    </group>

    <group
      name="YM"
      pcbX="0mm"
      pcbY="24mm"
    >
      <YM2149
        pcbX="-2mm"
        name="U_YM"
        schX={16}
        schY={0}
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
          IOA0: "net.YM_IOA0", // bank bit 0 -> PLD -> ROM A15
          IOA1: "net.YM_IOA1", // bank bit 1 -> PLD -> ROM A16
          IOA2: "net.YM_IOA2", // bank bit 2 -> PLD -> ROM A17
          IOA3: "net.YM_IOA3", // bank bit 3 -> PLD -> ROM A18
        }}
      />
      <capacitor
        name="C_YM"
        capacitance="0.1uF"
        footprint="axial_p7.62mm"
        schX={18}
        schY={5}
        pcbX="25mm"
        pcbY="0mm"
        pcbRotation={270}
        connections={{
          pin1: "net.VCC",
          pin2: "net.GND",
        }}
      />
      <resistor
        name="R_RESET"
        resistance="10k"
        footprint="axial_p7.62mm"
        schX={12}
        schY={10}
        pcbX="-18mm"
        pcbY="3mm"
        layer="bottom"
        connections={{
          pin1: "net.VCC",
          pin2: "net.RESET_DELAYED",
        }}
      />
      <capacitor
        name="C_RESET"
        capacitance="10uF"
        footprint="axial_p7.62mm"
        polarized
        schX={14}
        schY={7}
        pcbX="-18mm"
        pcbY="-3mm"
        layer="bottom"
        connections={{
          pin1: "net.RESET_DELAYED",
          pin2: "net.GND",
        }}
      />
      <resistor
        name="R_YM_AUDIOA"
        resistance="1k"
        footprint="axial_p7.62mm"
        pcbX="8mm"
        pcbY="11mm"
        schX={24}
        schY={5}
        connections={{
          pin1: "net.ANALOG_A",
          pin2: "net.SUM_NODE",
        }}
      />
      <resistor
        name="R_YM_AUDIOB"
        resistance="1k"
        footprint="axial_p7.62mm"
        pcbX="19mm"
        pcbY="11mm"
        schX={24}
        schY={2}
        connections={{
          pin1: "net.ANALOG_B",
          pin2: "net.SUM_NODE",
        }}
      />
      <resistor
        name="R_YM_AUDIOC"
        resistance="1k"
        footprint="axial_p7.62mm"
        pcbX="16mm"
        pcbY="-11.5mm"
        schX={24}
        schY={-1}
        connections={{
          pin1: "net.ANALOG_C",
          pin2: "net.SUM_NODE",
        }}
      />
    </group>

    <group
      name="Amp"
    >
      <capacitor
        name="C_AMP"
        capacitance="0.1uF"
        footprint="axial_p7.62mm"
        pcbX="-7mm"
        pcbY="0mm"
        pcbRotation={90}
        schX={32}
        schY={-4}
        connections={{
          pin1: "net.VCC",
          pin2: "net.GND",
        }}
      />
      <resistor
        name="R_FB"
        resistance="1k"
        footprint="axial_p7.62mm"
        pcbX="0mm"
        pcbY="0mm"
        pcbRotation={0}
        schX={34}
        schY={6}
        layer="bottom"
        connections={{
          pin1: "net.SUM_NODE",
          pin2: "net.OPAMP_OUT",
        }}
      />
      <LM358
        name="U_AMP"
        pcbX="0mm"
        pcbY="0mm"
        schX={28}
        schY={2}
        pcbRotation={270}
        connections={{
          VCC: "net.VCC",
          GND: "net.GND",
          IN1_POS: "net.GND",         // Pin 3: Tied to Ground
          IN1_NEG: "net.SUM_NODE",    // Pin 2: Connected directly to summing node
          OUT1: "net.OPAMP_OUT",      // Pin 1: Op-amp Output
          IN2_POS: "net.GND",         // Pin 5: Unused section - input tied to GND
          IN2_NEG: "net.AMP_UNUSED_FB", // Pin 6: Unused section - shorted to output
          OUT2: "net.AMP_UNUSED_FB",   // Pin 7: Unity-gain follower (output = GND)
        }}
      />
      <resistor
        name="R_PULL"
        resistance="1k"
        footprint="axial_p7.62mm"
        pcbX="0mm"
        pcbY="7mm"
        pcbRotation={0}
        schX={34}
        schY={2}
        connections={{
          pin1: "net.OPAMP_OUT",
          pin2: "net.GND",
        }}
      />
      <resistor
        name="R_SERIES"
        resistance="1k"
        footprint="axial_p7.62mm"
        pcbX="8mm"
        pcbY="0mm"
        pcbRotation={90}
        schX={34}
        schY={0}
        connections={{
          pin1: "net.OPAMP_OUT",
          pin2: "net.CAP_PLUS",
        }}
      />
      <capacitor
        name="C_AUDIO_OUT"
        capacitance="10uF"
        footprint="axial_p7.62mm"
        polarized
        pcbX="12mm"
        pcbY="0mm"
        pcbRotation={90}
        schX={40}
        schY={2}
        connections={{
          pin1: "net.CAP_PLUS",     // Positive (+) from Series Resistor
          pin2: "net.OPAMP_OUT_AC", // Negative (-) to Exaudio / Console
        }}
      />
    </group>

    <silkscreentext
      text="Lokey 7800 YM v0.2"
      anchorAlignment="top_left"
      pcbX="-27mm"
      pcbY="38mm"
      fontSize="1.2mm"
    />
    <silkscreentext
      text="github.com/jbsohn/lokey-7800-ym"
      anchorAlignment="top_left"
      pcbX="-27mm"
      pcbY="36mm"
      fontSize="1.2mm"
    />
  </board>
);
