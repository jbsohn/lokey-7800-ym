# Atari 7800 YM2149 Project Makefile

# Configuration & Directories
BUILD_DIR     := build
SRC_DIR       := src
YM_DIR        := YmSamples
VGM_DIR       := VgmSamples
PREVIEW_FLAGS := -s 2 -f 5000

# Tools
YM2BIN        := tools/YmToBin.cs
VGM2BIN       := tools/VgmToBin.cs
DOTNET        := dotnet script
DASM          := dasm
SIGN          := /home/john/7800AsmDevKit/7800sign
DASM_FLAGS    := -I$(SRC_DIR) -I$(BUILD_DIR)

# Dynamic Asset
YM_SOURCES    := $(wildcard $(YM_DIR)/*.ym) $(wildcard $(YM_DIR)/*.YM)
VGM_SOURCES   := $(wildcard $(VGM_DIR)/*.vgm) $(wildcard $(VGM_DIR)/*.vgz)

# Map source filenames to build artifacts
ALL_MUSIC_BINS := $(foreach f,$(YM_SOURCES) $(VGM_SOURCES),$(BUILD_DIR)/$(notdir $(basename $(f))).bin)
MUSIC_ROMS     := $(ALL_MUSIC_BINS:.bin=.a78)
MUSIC_WAVS     := $(ALL_MUSIC_BINS:.bin=.wav)

# Fixed logic ROMs (Heartbeat, etc.)
FIXED_ROMS     := $(BUILD_DIR)/ym2149_heartbeat_main.a78 $(BUILD_DIR)/ym2149_melody_vbi.a78

# Core Targets
.PHONY: all help clean gal hw a78 bin wav

all: a78

a78: $(BUILD_DIR) $(MUSIC_ROMS) $(FIXED_ROMS)

bin: $(BUILD_DIR) $(ALL_MUSIC_BINS) $(FIXED_ROMS:.a78=.bin)

wav: $(BUILD_DIR) $(MUSIC_WAVS)

help:
	@echo "Atari 7800 YM2149 SDK"
	@echo "Targets:"
	@echo "  make a78    - Build full library of preview ROMs"
	@echo "  make wav    - Generate verification .wav files for all tracks"
	@echo "  make hw     - Build and sign all assets for real hardware"
	@echo "  make clean  - Wipe all build artifacts"

$(BUILD_DIR):
	@mkdir -p $(BUILD_DIR)

# Conversion Rules (Source -> Binary)

$(BUILD_DIR)/%.bin: $(YM_DIR)/%.ym $(YM2BIN) | $(BUILD_DIR)
	$(DOTNET) $(YM2BIN) $< -o $@ $(PREVIEW_FLAGS)

$(BUILD_DIR)/%.bin: $(YM_DIR)/%.YM $(YM2BIN) | $(BUILD_DIR)
	$(DOTNET) $(YM2BIN) $< -o $@ $(PREVIEW_FLAGS)

$(BUILD_DIR)/%.bin: $(VGM_DIR)/%.vgm $(VGM2BIN) | $(BUILD_DIR)
	$(DOTNET) $(VGM2BIN) $< -o $@ $(PREVIEW_FLAGS)

$(BUILD_DIR)/%.bin: $(VGM_DIR)/%.vgz $(VGM2BIN) | $(BUILD_DIR)
	$(DOTNET) $(VGM2BIN) $< -o $@ $(PREVIEW_FLAGS)

# 6. Assembly Rules (Logic + Asset -> ROM)
$(BUILD_DIR)/%.a78: $(SRC_DIR)/ym2149_player.asm $(BUILD_DIR)/%.bin | $(BUILD_DIR)
	@echo "  Assembling ROM: $*"
	@$(DASM) $< $(DASM_FLAGS) -DMUSIC_INC=\"$*.yminc\" -DMUSIC_BIN=\"$*.bin\" -f3 -o$@

$(BUILD_DIR)/ym2149_%.a78: $(SRC_DIR)/ym2149_%.asm | $(BUILD_DIR)
	@$(DASM) $< $(DASM_FLAGS) -f3 -o$@

# Verification Rules (Binary -> WAV)
$(BUILD_DIR)/%.wav: $(BUILD_DIR)/%.bin tools/BinToWav.cs | $(BUILD_DIR)
	@echo "  Generating WAV: $*"
	@$(DOTNET) tools/BinToWav.cs $< $@

# Utilities
gal: $(BUILD_DIR)
	@galette gal/rom.pld && galette gal/rom_ym.pld
	@mv *.jed $(BUILD_DIR)/ 2>/dev/null || true

hw: bin
	@for f in $(BUILD_DIR)/*.bin; do \
		echo "Signing $$f"; \
		$(SIGN) -w "$$f" && $(SIGN) -t "$$f" || true; \
	done

clean:
	@rm -rf $(BUILD_DIR)
	@rm -f src/*.wav samples/*.wav
