import { type ChipProps } from "tscircuit";

interface SolderJumperProps extends ChipProps {
  label?: string;
  labelL?: string;
  labelR?: string;
}

export const SolderJumper = ({ label, labelL, labelR, ...props }: SolderJumperProps) => (
  <chip
    {...props}
    pinLabels={{
      1: "L", // Option A (e.g. VCC)
      2: "C", // Center / Common (connects to ROM pin)
      3: "R", // Option B (e.g. Address line)
    }}
  >
    <footprint>
      {/* Three SMT pads close together for easy solder bridging */}
      <smtpad
        shape="rect"
        width="1.2mm"
        height="2.0mm"
        pcbX="-1.5mm"
        pcbY="0mm"
        portHints={["pin1"]}
      />
      <smtpad
        shape="rect"
        width="1.2mm"
        height="2.0mm"
        pcbX="0mm"
        pcbY="0mm"
        portHints={["pin2"]}
      />
      <smtpad
        shape="rect"
        width="1.2mm"
        height="2.0mm"
        pcbX="1.5mm"
        pcbY="0mm"
        portHints={["pin3"]}
      />
      {label && (
        <silkscreentext
          pcbX={0}
          pcbY={1.8}
          text={label}
          fontSize="0.8mm"
        />
      )}
      {labelL && (
        <silkscreentext
          pcbX={-1.5}
          pcbY={-1.8}
          text={labelL}
          fontSize="0.6mm"
        />
      )}
      {labelR && (
        <silkscreentext
          pcbX={1.5}
          pcbY={-1.8}
          text={labelR}
          fontSize="0.6mm"
        />
      )}
    </footprint>
  </chip>
);
