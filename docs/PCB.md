# PCB Design Workflow (tscircuit)

Unlike traditional hardware projects, this project uses a **Code-to-PCB** workflow. The "Source of Truth" for the schematic is the React-based circuit code found in `pcb/index.circuit.tsx`.

We leverage **tscircuit**, a TypeScript framework that allows us to define electronic components and layouts using React.

## Generated Schematic

> **NOTE:** This PDF is a generated preview intended for visual reference. Always refer to `pcb/index.circuit.tsx` for the authoritative design.

[Atari 7800 YM2149 Cartridge Schematic](schematic.pdf)

## Environment Setup

The PCB project requires **Node.js** and the **tscircuit CLI**.

1. **Install Dependencies**:
   ```bash
   cd pcb
   npm install
   ```

2. **Install CLI Tools**:
   ```bash
   npm install -g tscircuit
   ```

## Build & Preview

To live-preview the latest PCB design and footprints:

```bash
cd pcb
npm run dev
```

This launches the `tscircuit` viewer in your browser for real-time inspection of the schematic, layout, and 3D preview.
