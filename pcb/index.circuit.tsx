import Atari7800EdgeConnector, { ATARI_7800_CONNECTOR_OUTLINE } from "./Atari7800EdgeConnector";
import { ROM_27C256 } from "./ROM_27C256";
import { ATF16V8B } from "./ATF16V8B";
import { Latch74HCT373 } from "./74HCT373";
import { YM2149 } from "./YM2149";
import { LM358 } from "./LM358";

export default () => (
  <board
    autorouterEffortLevel="10x"
    outline={[
      { x: "-32mm", y: "55mm" },
      { x: "32mm", y: "55mm" },
      { x: "32mm", y: "-10mm" },
      ...ATARI_7800_CONNECTOR_OUTLINE,
      { x: "-32mm", y: "-10mm" },
    ]}
  >
    <net name="VCC" /><net name="GND" />
    <net name="ANALOG_A" /><net name="ANALOG_B" /><net name="ANALOG_C" />
    <net name="SUM_NODE" /><net name="CAP_PLUS" />
    <net name="OPAMP_FB" /><net name="RESET_DELAYED" />

    <copperpour layer="bottom" connectsTo="net.GND" />

    <trace from=".J1 > .VCC" to=".U2 > .VCC" thickness="0.4mm" />
    <trace from=".J1 > .GND" to=".U2 > .GND" thickness="0.4mm" />
    <trace from=".U2 > .VCC" to=".U3 > .VCC" thickness="0.4mm" />
    <trace from=".U3 > .VCC" to=".U1 > .VCC" thickness="0.4mm" />
    <trace from=".U1 > .VCC" to=".U4 > .VCC" thickness="0.4mm" />
    <trace from=".U4 > .VCC" to=".U5 > .VCC" thickness="0.4mm" />

    {/* Explicit signal traces for routes the autorouter can't solve */}
    <trace from=".U3 > .Q4" to=".U4 > .DA4" thickness="0.15mm" />
    <trace from=".U2 > .A0" to=".U1 > .A0" thickness="0.15mm" />

    {/* --- Edge Connector --- */}
    <Atari7800EdgeConnector
      name="J1"
      pcbX="0mm" pcbY="-30.24mm"
      schX={-4} schY={0}
      connections={{
        VCC: "net.VCC", GND: "net.GND", "30": "net.GND",
        A0:"net.A0",A1:"net.A1",A2:"net.A2",A3:"net.A3",A4:"net.A4",
        A5:"net.A5",A6:"net.A6",A7:"net.A7",A8:"net.A8",A9:"net.A9",
        A10:"net.A10",A11:"net.A11",A12:"net.A12",A13:"net.A13",A14:"net.A14",A15:"net.A15",
        D0:"net.D0",D1:"net.D1",D2:"net.D2",D3:"net.D3",D4:"net.D4",
        D5:"net.D5",D6:"net.D6",D7:"net.D7",
        RW:"net.RW", HALT:"net.HALT", PHI2:"net.PHI2",
        Exaudio:"net.SUM_NODE",
      }}
    />

    {/* --- ROM (bottom, centered, rotated 270) --- */}
    <group name="ROM_Group" pcbX="0mm" pcbY="-4mm" pcbRotation={270}>
      <ROM_27C256
        name="U1" schX={8} schY={4}
        connections={{
          VCC:"net.VCC",VPP:"net.VCC",GND:"net.GND",OE:"net.GND",CE:"net.ROM_CE",
          A0:"net.A0",A1:"net.A1",A2:"net.A2",A3:"net.A3",A4:"net.A4",
          A5:"net.A5",A6:"net.A6",A7:"net.A7",A8:"net.A8",A9:"net.A9",
          A10:"net.A10",A11:"net.A11",A12:"net.A12",A13:"net.A13",A14:"net.A14",
          D0:"net.D0",D1:"net.D1",D2:"net.D2",D3:"net.D3",D4:"net.D4",
          D5:"net.D5",D6:"net.D6",D7:"net.D7",
        }}
      />
      <capacitor name="C1" capacitance="0.1uF" footprint="axial"
        pcbX="0mm" pcbY="21mm"
        connections={{ pin1:"net.VCC", pin2:"net.GND" }} />
    </group>

    {/* --- Logic (GAL + Latch, right side, unrotated) --- */}
    <group name="Logic_Group" pcbX="6mm" pcbY="14mm">
      <group name="GAL_Group" pcbX="-4mm" pcbY="0mm">
        <ATF16V8B
          name="U2" pcbX="0mm" pcbY="0mm" schX={2} schY={4}
          connections={{
            VCC:"net.VCC",GND:"net.GND",
            A15:"net.A15",A14:"net.A14",A0:"net.A0",
            HALT:"net.HALT",RW:"net.RW",PHI2:"net.PHI2",
            ROM_CE:"net.ROM_CE",BDIR:"net.BDIR",BC1:"net.BC1",
            PHI2OUT:"net.PHI2OUT",YM_LE:"net.YM_LE",
          }}
        />
        <capacitor name="C2" capacitance="0.1uF" footprint="axial"
          pcbX="0mm" pcbY="14.2mm"
          connections={{ pin1:"net.VCC", pin2:"net.GND" }} />
      </group>
      <group name="Latch_Group" pcbX="12mm" pcbY="0mm">
        <Latch74HCT373
          name="U3" pcbX="0mm" pcbY="0mm" schX={2} schY={-4}
          connections={{
            VCC:"net.VCC",GND:"net.GND",OE:"net.GND",LE:"net.YM_LE",
            D0:"net.D0",D1:"net.D1",D2:"net.D2",D3:"net.D3",
            D4:"net.D4",D5:"net.D5",D6:"net.D6",D7:"net.D7",
            Q0:"net.DA0",Q1:"net.DA1",Q2:"net.DA2",Q3:"net.DA3",
            Q4:"net.DA4",Q5:"net.DA5",Q6:"net.DA6",Q7:"net.DA7",
          }}
        />
        <capacitor name="C3" capacitance="0.1uF" footprint="axial"
          pcbX="0mm" pcbY="14.2mm"
          connections={{ pin1:"net.VCC", pin2:"net.GND" }} />
      </group>
    </group>

    {/* --- YM2149 PSG (left side, vertical) --- */}
    <YM2149
      name="U4"
      pcbX="-14mm" pcbY="28mm"
      schX={8} schY={-4}
      connections={{
        VCC:"net.VCC",BC2:"net.VCC",GND:"net.GND",
        DA0:"net.DA0",DA1:"net.DA1",DA2:"net.DA2",DA3:"net.DA3",
        DA4:"net.DA4",DA5:"net.DA5",DA6:"net.DA6",DA7:"net.DA7",
        CLK:"net.PHI2OUT",BDIR:"net.BDIR",BC1:"net.BC1",
        RESET:"net.RESET_DELAYED",A8:"net.VCC",A9:"net.GND",
        ANALOG_A:"net.ANALOG_A",ANALOG_B:"net.ANALOG_B",ANALOG_C:"net.ANALOG_C",
      }}
    />

    {/* RC Reset and C4 (left of YM2149) */}
    <resistor name="R_RESET" resistance="4.7k" footprint="axial"
      pcbX="-28mm" pcbY="36mm"
      connections={{ pin1:"net.VCC", pin2:"net.RESET_DELAYED" }} />
    <capacitor name="C_RESET" capacitance="10uF" footprint="axial" polarized
      pcbX="-28mm" pcbY="30mm"
      connections={{ pin1:"net.RESET_DELAYED", pin2:"net.GND" }} />
    <capacitor name="C4" capacitance="0.1uF" footprint="axial"
      pcbX="-28mm" pcbY="24mm"
      connections={{ pin1:"net.VCC", pin2:"net.GND" }} />

    {/* --- Audio Out (right of YM2149, right of logic) --- */}
    <group name="Audio_Out_Group" pcbX="14mm" pcbY="28mm">
      <resistor name="R1" resistance="1k" footprint="axial"
        pcbX="0mm" pcbY="6mm"
        connections={{ pin1:"net.ANALOG_A", pin2:"net.SUM_NODE" }} />
      <resistor name="R2" resistance="1k" footprint="axial"
        pcbX="0mm" pcbY="2mm"
        connections={{ pin1:"net.ANALOG_B", pin2:"net.SUM_NODE" }} />
      <resistor name="R3" resistance="1k" footprint="axial"
        pcbX="0mm" pcbY="-2mm"
        connections={{ pin1:"net.ANALOG_C", pin2:"net.SUM_NODE" }} />

      <LM358
        name="U5" pcbX="10mm" pcbY="0mm"
        connections={{
          VCC:"net.VCC",GND:"net.GND",
          IN1_POS:"net.GND",IN1_NEG:"net.OPAMP_FB",OUT1:"net.OPAMP_FB",
        }} />

      <resistor name="R_GRIT1" resistance="4.7k" footprint="axial"
        pcbX="10mm" pcbY="-8mm"
        connections={{ pin1:"net.OPAMP_FB", pin2:"net.GND" }} />
      <resistor name="R_GRIT2" resistance="4.7k" footprint="axial"
        pcbX="10mm" pcbY="-5mm"
        connections={{ pin1:"net.OPAMP_FB", pin2:"net.GND" }} />
      <resistor name="R_FEEDBACK" resistance="4.7k" footprint="axial"
        pcbX="10mm" pcbY="4mm"
        connections={{ pin1:"net.OPAMP_FB", pin2:"net.CAP_PLUS" }} />

      <capacitor name="C5" capacitance="10uF" footprint="axial" polarized
        pcbX="16mm" pcbY="4mm"
        connections={{ pin1:"net.CAP_PLUS", pin2:"net.SUM_NODE" }} />

      <capacitor name="C6" capacitance="0.1uF" footprint="axial"
        pcbX="10mm" pcbY="14mm"
        connections={{ pin1:"net.VCC", pin2:"net.GND" }} />
    </group>

    <silkscreentext text="7800 YM" pcbX="14mm" pcbY="50mm" fontSize="3mm" />
    <silkscreentext text="v1.0" pcbX="14mm" pcbY="46mm" fontSize="1.5mm" />
  </board>
);
