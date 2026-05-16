# PCB Design Workflow (tscircuit)

Unlike traditional hardware projects, this project uses a **Code-to-PCB** workflow. While the authoritative "Source of Truth" for hardware specifications is `docs/Hardware.md`, the implementation is managed via React-based circuit code in `pcb/index.circuit.tsx`.

We leverage **tscircuit**, a TypeScript framework that allows us to define electronic components and layouts using React.

## Generated Schematic

> **NOTE:** This PDF is a generated preview intended for visual reference. Always ensure that `pcb/index.circuit.tsx` matches the technical specifications in `docs/Hardware.md`.

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
