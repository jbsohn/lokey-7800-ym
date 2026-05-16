# Atari 7800 MADS Makefile
# Modeled after ca65/Makefile

# --- Configuration & Directories ---
BUILD_DIR     := build
BIN_DIR       := ../bin
YM_DIR        := ../ym-samples
VGM_DIR       := ../vgm-samples
ROOT_DIR      := ..

# --- Tool Flags ---
MADS_FLAGS    := -i:. -i:$(BUILD_DIR) -c

# --- Tools ---
YM2BIN        := $(BIN_DIR)/ymtoymb
VGM2BIN       := $(BIN_DIR)/vgmtoymb
A78GEN        := $(BIN_DIR)/a78gen
MADS          := mads

# --- Source Discovery ---
YM_SONGS      := $(basename $(notdir $(wildcard $(YM_DIR)/*.ym) $(wildcard $(YM_DIR)/*.YM)))
VGM_SONGS     := $(basename $(notdir $(wildcard $(VGM_DIR)/*.vgm) $(wildcard $(VGM_DIR)/*.vgz)))
SONGS         := $(sort $(YM_SONGS) $(VGM_SONGS))
DEMOS         := ym2149_heartbeat_main ym2149_melody_vbi

# --- Targets ---
.PHONY: all tools clean help $(SONGS) $(DEMOS)
.PRECIOUS: $(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi $(BUILD_DIR)/%.obx
.DELETE_ON_ERROR:

all: tools $(DEMOS) $(SONGS)

tools:
	@mkdir -p $(BIN_DIR)
	@echo "  Publishing Tools..."
	@dotnet publish $(ROOT_DIR)/tools/VgmToYmb/VgmToYmb.csproj -o $(BIN_DIR) --configuration Release --verbosity quiet
	@dotnet publish $(ROOT_DIR)/tools/YmToYmb/YmToYmb.csproj -o $(BIN_DIR) --configuration Release --verbosity quiet
	@dotnet publish $(ROOT_DIR)/tools/A78Gen/A78Gen.csproj -o $(BIN_DIR) --configuration Release --verbosity quiet

# Convenience Targets
$(SONGS): %: $(BUILD_DIR)/%.a78
$(DEMOS): %: $(BUILD_DIR)/%.a78

# --- Asset Conversion Rules ---
$(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi: $(YM_DIR)/%.ym | $(BUILD_DIR)
	@echo "  Converting YM: $<"
	@$(YM2BIN) $< -o $(BUILD_DIR)/$*.ymb -s 2 -f 5000

$(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi: $(YM_DIR)/%.YM | $(BUILD_DIR)
	@echo "  Converting YM: $<"
	@$(YM2BIN) $< -o $(BUILD_DIR)/$*.ymb -s 2 -f 5000

$(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi: $(VGM_DIR)/%.vgm | $(BUILD_DIR)
	@echo "  Converting VGM: $<"
	@$(VGM2BIN) $< -o $(BUILD_DIR)/$*.ymb -s 2 -f 5000

$(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi: $(VGM_DIR)/%.vgz | $(BUILD_DIR)
	@echo "  Converting VGM: $<"
	@$(VGM2BIN) $< -o $(BUILD_DIR)/$*.ymb -s 2 -f 5000

# --- ROM Assembly Rules ---

# 1. Final .a78 Production
$(BUILD_DIR)/%.a78: $(BUILD_DIR)/%.bin header.json
	@echo "  Packaging ROM: $@"
	@$(A78GEN) $< header.json -o $@

# Songs: Need to include player + data. 
# We'll create a temporary wrapper to compile them together.
$(BUILD_DIR)/%.bin: ym2149_player.asm $(BUILD_DIR)/%-data.asm | $(BUILD_DIR)
	@echo "  Assembling Song: $@"
	@echo ' opt h- f+' > $(BUILD_DIR)/$*-song.asm
	@echo ' PLAYER_INC = 1' >> $(BUILD_DIR)/$*-song.asm
	@echo ' icl "ym2149_player.asm"' >> $(BUILD_DIR)/$*-song.asm
	@echo ' icl "$*-data.asm"' >> $(BUILD_DIR)/$*-song.asm
	@echo ' .align $$fff8, $$ff' >> $(BUILD_DIR)/$*-song.asm
	@echo ' dta b($$ff, $$83)' >> $(BUILD_DIR)/$*-song.asm
	@echo ' dta a(reset, reset, reset)' >> $(BUILD_DIR)/$*-song.asm
	@$(MADS) $(MADS_FLAGS) $(BUILD_DIR)/$*-song.asm -o:$@
	@rm $(BUILD_DIR)/$*-song.asm

# Song Data Objects (MADS source)
$(BUILD_DIR)/%-data.asm: $(BUILD_DIR)/%.ymi $(BUILD_DIR)/%.ymb | $(BUILD_DIR)
	@echo "  Preparing Data: $*"
	@echo ' icl "$*.ymi"' > $@
	@echo ' STEP = (PLAYER_HZ * 65536) / 60' >> $@
	@echo 'MusicData' >> $@
	@echo ' ins "$*.ymb"' >> $@

# Generic Assembly for demos
$(BUILD_DIR)/%.bin: %.asm | $(BUILD_DIR)
	@echo "  Assembling Demo: $*"
	@$(MADS) $(MADS_FLAGS) $< -o:$@

$(BUILD_DIR):
	@mkdir -p $(BUILD_DIR)

clean:
	@echo "  Cleaning..."
	@rm -rf $(BUILD_DIR)

help:
	@echo "Atari 7800 MADS Build System (Local)"
	@echo ""
	@echo "Usage:"
	@echo "  make <target_name> - Build a specific target"
	@echo "  make all           - Build everything"
	@echo "  make clean         - Remove build artifacts"
