# Atari 7800 ca65 Makefile
# Self-contained build for the ca65/ directory

# --- Configuration & Directories ---
BUILD_DIR     := build
BIN_DIR       := ../bin
YM_DIR        := ../ym-samples
VGM_DIR       := ../vgm-samples
ROOT_DIR      := ..

# --- Tool Flags ---
CA65_FLAGS    := -I . -I $(BUILD_DIR) --bin-include-dir $(BUILD_DIR) -t none -g --debug-info -W 1
LD65_FLAGS    := -C 7800.cfg
# Dependency generation flags
DEP_FLAGS     = --create-dep $(@:.o=.d)

# --- Tools ---
YM2BIN        := $(BIN_DIR)/ymtoymb
VGM2BIN       := $(BIN_DIR)/vgmtoymb
A78GEN        := $(BIN_DIR)/a78gen
CA65          := ca65
LD65          := ld65

# --- Source Discovery ---
YM_SONGS      := $(basename $(notdir $(wildcard $(YM_DIR)/*.ym) $(wildcard $(YM_DIR)/*.YM)))
VGM_SONGS     := $(basename $(notdir $(wildcard $(VGM_DIR)/*.vgm) $(wildcard $(VGM_DIR)/*.vgz)))
SONGS         := $(sort $(YM_SONGS) $(VGM_SONGS))
DEMOS         := ym2149_heartbeat_main ym2149_melody_vbi

# --- Targets ---
.PHONY: all tools clean help $(SONGS) $(DEMOS)
.PRECIOUS: $(BUILD_DIR)/%.ymb $(BUILD_DIR)/%.ymi $(BUILD_DIR)/%.o
.DELETE_ON_ERROR:

all: tools $(DEMOS) $(SONGS)

# Include generated dependencies
-include $(wildcard $(BUILD_DIR)/*.d)

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

# 2. Linking
# Songs: Link player with specific song data (detected by -data.o dependency)
$(SONGS:%=$(BUILD_DIR)/%.bin): $(BUILD_DIR)/%.bin: $(BUILD_DIR)/ym2149_player.o $(BUILD_DIR)/%-data.o
	@echo "  Linking Song: $@"
	@$(LD65) $(LD65_FLAGS) $^ -o $@ -m $(@:.bin=.map)

# Use a regular pattern for demos
$(BUILD_DIR)/%.bin: $(BUILD_DIR)/%.o
	@echo "  Linking Demo: $@"
	@$(LD65) $(LD65_FLAGS) $< -o $@ -m $(@:.bin=.map)

# 3. Assembling
# Compiled Player (Reusable)
$(BUILD_DIR)/ym2149_player.o: ym2149_player.asm | $(BUILD_DIR)
	@echo "  Assembling Player: $<"
	@$(CA65) $(CA65_FLAGS) $(DEP_FLAGS) $< -o $@

# Song Data Objects (Linkable Assets)
$(BUILD_DIR)/%-data.o: $(BUILD_DIR)/%.ymi $(BUILD_DIR)/%.ymb | $(BUILD_DIR)
	@echo "  Assembling Data: $*"
	@echo '.macro export_ymi file' > $(BUILD_DIR)/$*-data.s
	@echo '  .include file' >> $(BUILD_DIR)/$*-data.s
	@echo '  .export PATTERN_SIZE, PLAYER_HZ, MAX_FRAMES, YM_DELAY, YM_FINE, NUM_PATTERNS, SEQ_LEN' >> $(BUILD_DIR)/$*-data.s
	@echo '.endmacro' >> $(BUILD_DIR)/$*-data.s
	@echo 'export_ymi "$*.ymi"' >> $(BUILD_DIR)/$*-data.s
	@echo '.segment "RODATA"' >> $(BUILD_DIR)/$*-data.s
	@echo '.export MusicData' >> $(BUILD_DIR)/$*-data.s
	@echo 'MusicData: .incbin "$*.ymb"' >> $(BUILD_DIR)/$*-data.s
	@$(CA65) $(CA65_FLAGS) $(BUILD_DIR)/$*-data.s -o $@
	@rm $(BUILD_DIR)/$*-data.s

# Generic Assembly for demos
$(BUILD_DIR)/%.o: %.asm | $(BUILD_DIR)
	@echo "  Assembling Demo: $*"
	@$(CA65) $(CA65_FLAGS) $(DEP_FLAGS) $< -o $@

$(BUILD_DIR):
	@mkdir -p $(BUILD_DIR)

clean:
	@echo "  Cleaning..."
	@rm -rf $(BUILD_DIR)

help:
	@echo "Atari 7800 ca65 Build System (Local)"
	@echo ""
	@echo "Usage:"
	@echo "  make <target_name> - Build a specific target"
	@echo "  make all           - Build everything"
	@echo "  make clean         - Remove build artifacts"
