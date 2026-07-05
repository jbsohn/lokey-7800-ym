# Atari 7800 YM2149 Project Makefile

# --- Build Configuration ---
# Options: dasm, mads
ASSEMBLER ?= dasm

# --- Configuration & Directories ---
BUILD_DIR     := build
BIN_DIR       := bin
SRC_DIR       := examples
YM_DIR        := ym-samples
VGM_DIR       := vgm-samples
PREVIEW_FLAGS := -s 2 -f 5000

# --- Tool Mapping ---
YM2BIN        := $(BIN_DIR)/ymtoymb
VGM2BIN       := $(BIN_DIR)/vgmtoymb
WAVTOOL       := $(BIN_DIR)/ymbtowav
A78GEN        := $(BIN_DIR)/a78gen
SIGN          := 7800sign

# --- OS Detection & KiCad Setup ---
UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S),Darwin)
  KICAD_APP    ?= /Applications/KiCad/KiCad.app
  KICAD_PYTHON ?= $(KICAD_APP)/Contents/Frameworks/Python.framework/Versions/3.9/bin/python3
  export PATH  := $(KICAD_APP)/Contents/MacOS:$(PATH)
else
  KICAD_PYTHON ?= python3
endif

# --- Assembler Setup ---
ifeq ($(ASSEMBLER),mads)
  ASM_CMD   := mads
  ASM_FLAGS := -i:include -i:$(BUILD_DIR) -f -fv:255 -c -d:MADS=1
  ASM_OUT   := -o:
  DEF_OPT   := -d:
else
  ASM_CMD   := dasm
  ASM_FLAGS := -Iinclude -I$(BUILD_DIR) -f3 -DMADS=0
  ASM_OUT   := -o
  DEF_OPT   := -D
endif

# --- Dynamic Asset Discovery ---
YM_SOURCES    := $(wildcard $(YM_DIR)/*.ym) $(wildcard $(YM_DIR)/*.YM)
VGM_SOURCES   := $(wildcard $(VGM_DIR)/*.vgm) $(wildcard $(VGM_DIR)/*.vgz)

ALL_MUSIC_DATA := $(foreach f,$(YM_SOURCES) $(VGM_SOURCES),$(BUILD_DIR)/$(notdir $(basename $(f))).ymb)
MUSIC_A78S     := $(ALL_MUSIC_DATA:.ymb=.a78)
MUSIC_ROMS     := $(ALL_MUSIC_DATA:.ymb=.rom)
MUSIC_WAVS     := $(ALL_MUSIC_DATA:.ymb=.wav)

# Demo Discovery
FIXED_BASE     := triad
PRO_BASE       := triad_mads

FIXED_A78S     := $(foreach f,$(FIXED_BASE),$(BUILD_DIR)/$(f).a78)
PRO_A78S       := $(foreach f,$(PRO_BASE),$(BUILD_DIR)/$(f).a78)
FIXED_ROMS     := $(foreach f,$(FIXED_BASE),$(BUILD_DIR)/$(f).rom)

# --- Core Targets ---
.PHONY: all help clean logic rom a78 bin wav tools pro pcb pcb-28pin pcb-32pin-max schematic previews

all: tools a78


pcb-28pin:
	@echo "Exporting and autorouting 28-pin PCB from tscircuit..."
	@cd pcb && $(KICAD_PYTHON) ./route_and_patch.py 28pin.circuit.tsx
	@$(MAKE) schematic-28pin
	@$(MAKE) previews

pcb-32pin-max:
	@echo "Exporting and autorouting 32-pin-max PCB from tscircuit..."
	@cd pcb && $(KICAD_PYTHON) ./route_and_patch.py 32pin-max.circuit.tsx
	@$(MAKE) schematic-32pin-max
	@$(MAKE) previews

pcb: pcb-32pin-max

schematic-28pin:
	@echo "Exporting 28-pin schematic SVG..."
	@mkdir -p $(BUILD_DIR)
	@cd pcb && npx tsci export -f schematic-svg 28pin.circuit.tsx -o ../docs/schematic-28pin.svg

schematic-32pin-max:
	@echo "Exporting 32-pin-max schematic SVG..."
	@mkdir -p $(BUILD_DIR)
	@cd pcb && npx tsci export -f schematic-svg 32pin-max.circuit.tsx -o ../docs/schematic-32pin-max.svg

schematic: schematic-32pin-max

previews:
	@echo "Exporting PCB SVG previews from KiCad..."
	@kicad-cli pcb export svg --mode-single --layers F.Cu,F.Silkscreen,F.Mask,Edge.Cuts --exclude-drawing-sheet --fit-page-to-board -o docs/pcb_front.svg pcb/build/KiCad/index.kicad_pcb
	@kicad-cli pcb export svg --mode-single --layers B.Cu,B.Silkscreen,B.Mask,Edge.Cuts --exclude-drawing-sheet --fit-page-to-board --mirror -o docs/pcb_back.svg pcb/build/KiCad/index.kicad_pcb

# The 'pro' target specifically builds the MADS-only showcase
pro:
	@$(MAKE) a78 ASSEMBLER=mads FIXED_BASE="$(PRO_BASE)"

# --- Tool Build Rules (Optimized to skip if already built) ---

tools: $(YM2BIN) $(VGM2BIN) $(WAVTOOL) $(A78GEN)

$(YM2BIN): tools/YmToYmb/*.cs tools/Core/*.cs
	@echo "  Building YmToYmb..."
	@mkdir -p $(BIN_DIR)
	@dotnet publish tools/YmToYmb/YmToYmb.csproj -o $(BIN_DIR) --configuration Release --verbosity quiet

$(VGM2BIN): tools/VgmToYmb/*.cs tools/Core/*.cs
	@echo "  Building VgmToYmb..."
	@mkdir -p $(BIN_DIR)
	@dotnet publish tools/VgmToYmb/VgmToYmb.csproj -o $(BIN_DIR) --configuration Release --verbosity quiet

$(WAVTOOL): tools/YmbToWav/*.cs tools/Core/*.cs
	@echo "  Building YmbToWav..."
	@mkdir -p $(BIN_DIR)
	@dotnet publish tools/YmbToWav/YmbToWav.csproj -o $(BIN_DIR) --configuration Release --verbosity quiet

$(A78GEN): tools/A78Gen/*.cs tools/Core/*.cs
	@echo "  Building A78Gen..."
	@mkdir -p $(BIN_DIR)
	@dotnet publish tools/A78Gen/A78Gen.csproj -o $(BIN_DIR) --configuration Release --verbosity quiet

rom: $(BUILD_DIR) $(MUSIC_ROMS) $(FIXED_ROMS)
	@for f in $(BUILD_DIR)/*.rom; do \
		echo "Signing $$f"; \
		$(SIGN) -w "$$f" && $(SIGN) -t "$$f" || true; \
	done

a78: $(BUILD_DIR) $(MUSIC_A78S) $(FIXED_A78S)

bin: $(BUILD_DIR) $(ALL_MUSIC_DATA)

wav: $(BUILD_DIR) $(MUSIC_WAVS)

help:
	@echo "Atari 7800 YM2149 SDK"
	@echo "Assembler: $(ASSEMBLER) (Use 'make ASSEMBLER=mads' to switch)"
	@echo ""
	@echo "Targets:"
	@echo "  make tools     - Build the .NET music conversion tools"
	@echo "  make pcb-28pin     - Build 28-pin board PCB"
	@echo "  make pcb-32pin-max - Build 32-pin max board PCB"
	@echo "  make pcb           - Build PCB for current BOARD_TARGET (default: 32pin-max)"
	@echo "  make previews  - Export front/back SVG previews of the current PCB design"
	@echo "  make logic     - Build the ATF16V8B logic files (.jed)"
	@echo "  make a78       - Build library of preview ROMs (emulator format)"
	@echo "  make pro       - Build the MADS 'Pro' showcase demo"
	@echo "  make rom       - Build and sign raw ROMs for hardware (.rom)"
	@echo "  make wav       - Generate verification .wav files for all tracks"
	@echo "  make clean     - Wipe all build artifacts"

$(BUILD_DIR):
	@mkdir -p $(BUILD_DIR)

# --- Conversion Rules ---

$(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi: $(YM_DIR)/%.ym $(YM2BIN) | $(BUILD_DIR)
	@$(YM2BIN) $< -o $@ $(PREVIEW_FLAGS)

$(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi: $(YM_DIR)/%.YM $(YM2BIN) | $(BUILD_DIR)
	@$(YM2BIN) $< -o $@ $(PREVIEW_FLAGS)

$(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi: $(VGM_DIR)/%.vgm $(VGM2BIN) | $(BUILD_DIR)
	@$(VGM2BIN) $< -o $@ $(PREVIEW_FLAGS)

$(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi: $(VGM_DIR)/%.vgz $(VGM2BIN) | $(BUILD_DIR)
	@$(VGM2BIN) $< -o $@ $(PREVIEW_FLAGS)

# --- Assembly Rules ---

$(BUILD_DIR)/%.a78: $(BUILD_DIR)/%.bin header.json $(A78GEN)
	@echo "  Packaging ROM [$(ASSEMBLER)]: $@"
	@$(A78GEN) $< header.json -o $@

# Music Player (Uses universal source)
$(BUILD_DIR)/%.bin: $(SRC_DIR)/player.asm $(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi | $(BUILD_DIR)
	@echo "  Assembling Player ROM [$(ASSEMBLER)]: $*"
	@mkdir -p $(BUILD_DIR)/$*_inc
ifeq ($(ASSEMBLER),mads)
	@echo ' icl "$*.ymi"' > $(BUILD_DIR)/$*_inc/music_bin.inc
	@echo 'music_data: ins "$*.ymb"' >> $(BUILD_DIR)/$*_inc/music_bin.inc
	@$(ASM_CMD) $< $(ASM_FLAGS) -i:$(BUILD_DIR)/$*_inc $(ASM_OUT)$@
else
	@echo ' include "$*.ymi"' > $(BUILD_DIR)/$*_inc/music_bin.inc
	@echo 'music_data: incbin "$*.ymb"' >> $(BUILD_DIR)/$*_inc/music_bin.inc
	@$(ASM_CMD) $< $(ASM_FLAGS) -I$(BUILD_DIR)/$*_inc $(ASM_OUT)$@
endif
	@rm -rf $(BUILD_DIR)/$*_inc

# Generic Demo rule
$(BUILD_DIR)/%.bin: $(SRC_DIR)/%.asm | $(BUILD_DIR)
	@echo "  Assembling Demo ROM [$(ASSEMBLER)]: $*"
	@$(ASM_CMD) $< $(ASM_FLAGS) $(ASM_OUT)$@

# Legacy Hardware format
$(BUILD_DIR)/%.rom: $(BUILD_DIR)/%.bin
	@cp $< $@

# --- Utilities ---
logic: $(BUILD_DIR)
	@echo "Building 28-pin and 32-pin board PLD JED files from .pld sources..."
	@galette gal/rom_28pin.pld && galette gal/rom_ym_28pin.pld
	@galette gal/rom_32pin.pld && galette gal/rom_ym_32pin.pld
	@mv gal/*.jed $(BUILD_DIR)/ 2>/dev/null || true

clean:
	@rm -rf $(BUILD_DIR)
	@rm -rf $(BIN_DIR)
	@rm -f $(BUILD_DIR)/*.wav $(YM_DIR)/*.wav $(VGM_DIR)/*.wav
	@rm -rf pcb/build/
