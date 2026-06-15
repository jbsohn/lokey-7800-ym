#!/usr/bin/env node
import { execSync } from "child_process";
import { readFileSync, writeFileSync, existsSync, rmSync } from "fs";

process.chdir(import.meta.dirname);

const PCB_PATH = "./KiCad/index.kicad_pcb";
const DSN_PATH = "./KiCad/index.dsn";
const SES_PATH = "./KiCad/index.ses";
const GERBER_DIR = "./KiCad/gerbers/";

const PYTHON_BIN = process.platform === "darwin"
  ? "/Applications/KiCad/KiCad.app/Contents/Frameworks/Python.framework/Versions/Current/bin/python3"
  : "python3";

console.log("1. Exporting unrouted board from tscircuit React...");
execSync("npx tsci export index.circuit.tsx -f kicad_pcb -o " + PCB_PATH, { stdio: "inherit" });

console.log("2. Patching PCB design settings via KiCad Python API...");
execSync(`${PYTHON_BIN} ../scripts/patch_pcb.py ${PCB_PATH}`, { stdio: "inherit" });

console.log("3. Exporting board to Specctra DSN...");
const exportCmd = `${PYTHON_BIN} -c "import pcbnew; board = pcbnew.LoadBoard('${PCB_PATH}'); pcbnew.ExportSpecctraDSN(board, '${DSN_PATH}')"`;
execSync(exportCmd, { stdio: "inherit" });

console.log("4. Patching DSN rules and boundary...");
let dsn = readFileSync(DSN_PATH, "utf8");

// Patch the global rule block
const global_rule_old = `    (rule
      (width 200)
      (clearance 200)
      (clearance 50 (type smd_smd))
    )`;
const global_rule_new = `    (rule
      (width 200)
      (clearance 200)
      (clearance 50 (type smd_smd))
      (clearance 0 (type smd_pcb))
      (clearance 0 (type pcb))
    )`;
if (dsn.includes(global_rule_old)) {
  dsn = dsn.replace(global_rule_old, global_rule_new);
}

// Patch the class rule block
const class_rule_old = `      (rule
        (width 200)
        (clearance 200)
      )`;
const class_rule_new = `      (rule
        (width 200)
        (clearance 200)
        (clearance 0 (type smd_pcb))
        (clearance 0 (type pcb))
      )`;
if (dsn.includes(class_rule_old)) {
  dsn = dsn.replace(class_rule_old, class_rule_new);
}

// Patch boundary bottom edge coordinates from -140000 to -140200
const boundaryStart = dsn.indexOf("(boundary");
if (boundaryStart !== -1) {
  let boundaryEnd = dsn.indexOf(")", boundaryStart);
  let parenCount = 0;
  for (let i = boundaryStart; i < dsn.length; i++) {
    if (dsn[i] === '(') parenCount++;
    else if (dsn[i] === ')') {
      parenCount--;
      if (parenCount === 0) {
        boundaryEnd = i + 1;
        break;
      }
    }
  }
  const boundaryBlock = dsn.slice(boundaryStart, boundaryEnd);
  const newBoundaryBlock = boundaryBlock.replace(/-140000/g, "-140200");
  dsn = dsn.slice(0, boundaryStart) + newBoundaryBlock + dsn.slice(boundaryEnd);
}

writeFileSync(DSN_PATH, dsn);

console.log("5. Running freerouting...");
if (existsSync(SES_PATH)) {
  try {
    rmSync(SES_PATH);
  } catch (e) {}
}

execSync(`freerouting -de ${DSN_PATH} -do ${SES_PATH} -mp 10`, { stdio: "inherit" });

if (!existsSync(SES_PATH)) {
  console.error("Error: freerouting failed to generate session file.");
  process.exit(1);
}

console.log("6. Importing session routing back into KiCad...");
const importCmd = `${PYTHON_BIN} -c "import pcbnew; board = pcbnew.LoadBoard('${PCB_PATH}'); pcbnew.ImportSpecctraSES(board, '${SES_PATH}'); board.Save('${PCB_PATH}')"`;
execSync(importCmd, { stdio: "inherit" });

console.log("7. Refilling zones and running DRC...");
execSync(`kicad-cli pcb drc --refill-zones --save-board ${PCB_PATH}`, { stdio: "inherit" });

console.log("8. Exporting Gerbers and Drill files...");
execSync(`kicad-cli pcb export gerbers -o ${GERBER_DIR} ${PCB_PATH}`, { stdio: "inherit" });
execSync(`kicad-cli pcb export drill -o ${GERBER_DIR} ${PCB_PATH}`, { stdio: "inherit" });

console.log("\nSuccess! Fully routed KiCad PCB and Gerbers are updated.");
