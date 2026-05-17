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
.PHONY: all help clean logic rom a78 bin wav tools pro

all: tools a78

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
	@echo "  make tools  - Build the .NET music conversion tools"
	@echo "  make logic  - Build the ATF16V8B logic files (.jed)"
	@echo "  make a78    - Build library of preview ROMs (emulator format)"
	@echo "  make pro    - Build the MADS 'Pro' showcase demo"
	@echo "  make rom    - Build and sign raw ROMs for hardware (.rom)"
	@echo "  make wav    - Generate verification .wav files for all tracks"
	@echo "  make clean  - Wipe all build artifacts"

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
	@echo "Building ATF16V8B JED files from .pld sources..."
	@galette gal/rom.pld && galette gal/rom_ym.pld
	@mv gal/*.jed $(BUILD_DIR)/ 2>/dev/null || true

clean:
	@rm -rf $(BUILD_DIR)
	@rm -rf $(BIN_DIR)
	@rm -f $(BUILD_DIR)/*.wav $(YM_DIR)/*.wav $(VGM_DIR)/*.wav
