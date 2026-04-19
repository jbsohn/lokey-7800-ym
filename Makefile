# Atari 7800 YM2149 Project Makefile

# Configuration & Directories
BUILD_DIR     := build
BIN_DIR       := bin
SRC_DIR       := sample-code
YM_DIR        := ym-samples
VGM_DIR       := vgm-samples
PREVIEW_FLAGS := -s 2 -f 5000

# Tools
YM2BIN        := $(BIN_DIR)/ymtoymb
VGM2BIN       := $(BIN_DIR)/vgmtoymb
WAVTOOL       := $(BIN_DIR)/ymbtowav
YMS           := $(BIN_DIR)/ymscompile
YMSTOWAV      := $(BIN_DIR)/ymstowav
DASM          := dasm
SIGN          := 7800sign
DASM_FLAGS    := -I$(SRC_DIR) -I$(BUILD_DIR)

# Dynamic Asset
YM_SOURCES    := $(wildcard $(YM_DIR)/*.ym) $(wildcard $(YM_DIR)/*.YM)
VGM_SOURCES   := $(wildcard $(VGM_DIR)/*.vgm) $(wildcard $(VGM_DIR)/*.vgz)
SFX_SOURCES   := $(wildcard ym-sounds/*.json)

# Map source filenames to build artifacts
ALL_MUSIC_DATA := $(foreach f,$(YM_SOURCES) $(VGM_SOURCES),$(BUILD_DIR)/$(notdir $(basename $(f))).ymb)
MUSIC_A78S     := $(ALL_MUSIC_DATA:.ymb=.a78)
MUSIC_ROMS     := $(ALL_MUSIC_DATA:.ymb=.rom)
MUSIC_WAVS     := $(ALL_MUSIC_DATA:.ymb=.wav)

SFX_DATA       := $(foreach f,$(SFX_SOURCES),$(BUILD_DIR)/$(notdir $(basename $(f))).yms)
SFX_WAVS       := $(foreach f,$(SFX_SOURCES),$(BUILD_DIR)/$(notdir $(basename $(f)))_sfx.wav)

# Fixed logic ROMs (Heartbeat, etc.)
FIXED_BASE     := ym2149_heartbeat_main ym2149_melody_vbi ym2149_concurrent_sfx ym2149_sfx_gallery
FIXED_A78S     := $(foreach f,$(FIXED_BASE),$(BUILD_DIR)/$(f).a78)
FIXED_ROMS     := $(foreach f,$(FIXED_BASE),$(BUILD_DIR)/$(f).rom)

# Core Targets
.PHONY: all help clean gal rom a78 bin wav tools sfx gallery

all: tools a78 sfx gallery

gallery: $(BUILD_DIR)/ym2149_sfx_gallery.a78

$(BUILD_DIR)/ym2149_sfx_gallery.a78: $(SRC_DIR)/ym2149_sfx_gallery.asm $(BUILD_DIR)/all_sounds.asm | $(BUILD_DIR)
	@echo "  Assembling Emulator ROM: sfx_gallery"
	@$(DASM) $< $(DASM_FLAGS) -Dbuild_with_header=1 -f3 -o$@

$(BUILD_DIR)/all_sounds.asm: $(SFX_SOURCES) laser.json $(YMS) | $(BUILD_DIR)
	@echo "  Compiling SFX Gallery include..."
	@for f in ym-sounds/*.json; do $(YMS) $$f -asm -o $(BUILD_DIR)/$$(basename $$f .json).asm; done
	@$(YMS) laser.json -asm -o $(BUILD_DIR)/laser.asm
	@cat $(BUILD_DIR)/coin.asm $(BUILD_DIR)/explosion.asm $(BUILD_DIR)/fail.asm $(BUILD_DIR)/powerup.asm $(BUILD_DIR)/laser.asm > $@

# Build the tools solution into the bin directory using Native AOT
tools:
	@mkdir -p $(BIN_DIR)
	dotnet publish tools/VgmToYmb/VgmToYmb.csproj -o $(BIN_DIR) --configuration Release
	dotnet publish tools/YmToYmb/YmToYmb.csproj -o $(BIN_DIR) --configuration Release
	dotnet publish tools/YmbToWav/YmbToWav.csproj -o $(BIN_DIR) --configuration Release
	dotnet publish tools/YmsCompile/YmsCompile.csproj -o $(BIN_DIR) --configuration Release
	dotnet publish tools/YmsToWav/YmsToWav.csproj -o $(BIN_DIR) --configuration Release
	dotnet publish tools/YmTestDrive/YmTestDrive.csproj -o $(BIN_DIR) --configuration Release

$(YM2BIN) $(VGM2BIN) $(WAVTOOL) $(YMS) $(YMSTOWAV):
	@$(MAKE) tools

rom: $(BUILD_DIR) $(MUSIC_ROMS) $(FIXED_ROMS)
	@for f in $(BUILD_DIR)/*.rom; do \
		echo "Signing $$f"; \
		$(SIGN) -w "$$f" && $(SIGN) -t "$$f" || true; \
	done

sfx: $(SFX_DATA) $(SFX_WAVS)

a78: $(BUILD_DIR) $(MUSIC_A78S) $(FIXED_A78S)

bin: $(BUILD_DIR) $(ALL_MUSIC_DATA)

wav: $(BUILD_DIR) $(MUSIC_WAVS)

help:
	@echo "Atari 7800 YM2149 SDK"
	@echo "Targets:"
	@echo "  make tools  - Build the .NET music conversion tools"
	@echo "  make a78    - Build full library of preview ROMs (emulator format)"
	@echo "  make rom    - Build and sign raw ROMs for hardware (.rom)"
	@echo "  make wav    - Generate verification .wav files for all tracks"
	@echo "  make clean  - Wipe all build artifacts"

$(BUILD_DIR):
	@mkdir -p $(BUILD_DIR)

# Conversion Rules (Source -> Music Data)

$(BUILD_DIR)/%.yms: ym-sounds/%.json $(YMS) | $(BUILD_DIR)
	$(YMS) $< -o $@

$(BUILD_DIR)/%_sfx.wav: $(BUILD_DIR)/%.yms $(YMSTOWAV) | $(BUILD_DIR)
	$(YMSTOWAV) $< -o $@

$(BUILD_DIR)/%.ymb: $(YM_DIR)/%.ym $(YM2BIN) | $(BUILD_DIR)
	$(YM2BIN) $< -o $@ $(PREVIEW_FLAGS)

$(BUILD_DIR)/%.ymb: $(YM_DIR)/%.YM $(YM2BIN) | $(BUILD_DIR)
	$(YM2BIN) $< -o $@ $(PREVIEW_FLAGS)

$(BUILD_DIR)/%.ymb: $(VGM_DIR)/%.vgm $(VGM2BIN) | $(BUILD_DIR)
	$(VGM2BIN) $< -o $@ $(PREVIEW_FLAGS)

$(BUILD_DIR)/%.ymb: $(VGM_DIR)/%.vgz $(VGM2BIN) | $(BUILD_DIR)
	$(VGM2BIN) $< -o $@ $(PREVIEW_FLAGS)

# Assembly Rules (Logic + Music Data -> ROM)

# Emulator format (.a78) with header
$(BUILD_DIR)/%.a78: $(SRC_DIR)/ym2149_player.asm $(BUILD_DIR)/%.ymb | $(BUILD_DIR)
	@echo "  Assembling Emulator ROM: $*"
	@$(DASM) $< $(DASM_FLAGS) -Dbuild_with_header=1 -DMUSIC_INC=\"$*.ymi\" -DMUSIC_BIN=\"$*.ymb\" -f3 -o$@

$(BUILD_DIR)/ym2149_concurrent_sfx.a78: $(SRC_DIR)/ym2149_concurrent_sfx.asm $(BUILD_DIR)/AY-3-8910_Jam.ymb | $(BUILD_DIR)
	@echo "  Assembling Emulator ROM: concurrent_sfx"
	@$(DASM) $< $(DASM_FLAGS) -Dbuild_with_header=1 -f3 -o$@

$(BUILD_DIR)/ym2149_%.a78: $(SRC_DIR)/ym2149_%.asm | $(BUILD_DIR)
	@echo "  Assembling Emulator ROM: $*"
	@$(DASM) $< $(DASM_FLAGS) -Dbuild_with_header=1 -f3 -o$@

# Raw hardware format (.rom) without header
$(BUILD_DIR)/%.rom: $(SRC_DIR)/ym2149_player.asm $(BUILD_DIR)/%.ymb | $(BUILD_DIR)
	@echo "  Assembling Hardware ROM: $*"
	@$(DASM) $< $(DASM_FLAGS) -Dbuild_with_header=0 -DMUSIC_INC=\"$*.ymi\" -DMUSIC_BIN=\"$*.ymb\" -f3 -o$@

$(BUILD_DIR)/ym2149_%.rom: $(SRC_DIR)/ym2149_%.asm | $(BUILD_DIR)
	@echo "  Assembling Hardware ROM: $*"
	@$(DASM) $< $(DASM_FLAGS) -Dbuild_with_header=0 -f3 -o$@

# Verification Rules (Music Data -> WAV)
$(BUILD_DIR)/%.wav: $(BUILD_DIR)/%.ymb $(WAVTOOL) | $(BUILD_DIR)
	@echo "  Generating WAV: $*"
	@$(WAVTOOL) $< $@

# Utilities
gal: $(BUILD_DIR)
	@galette gal/rom.pld && galette gal/rom_ym.pld
	@mv *.jed $(BUILD_DIR)/ 2>/dev/null || true

clean:
	@rm -rf $(BUILD_DIR)
	@rm -rf $(BIN_DIR)
	@rm -f src/*.wav ym-sounds/*.wav samples/*.wav
