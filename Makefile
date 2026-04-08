# Atari 7800 YM2149 Project Makefile

# Find all 6502 assembly sources
ASM_SOURCES := $(wildcard src/ym2149_*.asm)
ASM_BASENAMES := $(notdir $(ASM_SOURCES))
BIN_OUTPUTS := $(ASM_BASENAMES:.asm=.bin)
A78_OUTPUTS := $(ASM_BASENAMES:.asm=.a78)

# Tools
SIGN_TOOL := /home/john/7800AsmDevKit/7800sign
DASM_FLAGS := -Isrc
YM_TOOL    := tools/ym-tool.sh
GAL_SOURCE := gal/rom_ym.pld

.PHONY: all help a78 bin sign hw gal clean process-test process-stress

all: a78

help:
	@echo "Build Targets:"
	@echo "  make a78           - Build .a78 ROMs for all ASM sources"
	@echo "  make bin           - Build headerless .bin ROMs"
	@echo "  make hw            - Build and sign .bin ROMs for real hardware"
	@echo "  make gal           - Build JEDEC using galette"
	@echo "  make process-test   - Generate ym_test_data.bin (C# Tool)"
	@echo "  make process-stress - Generate test1.bin stress test (C# Tool)"
	@echo "  make clean         - Nuke all generated files"

a78: $(A78_OUTPUTS)

bin: $(BIN_OUTPUTS)

# Pattern rules for DASM
%.a78: src/%.asm src/a78_ym2149_header.asm
	dasm $< $(DASM_FLAGS) -f3 -o$@

%.bin: src/%.asm src/a78_ym2149_header.asm
	dasm $< $(DASM_FLAGS) -Dbuild_with_header=0 -f3 -o$@

# Signing for real hardware
sign: bin
	@for f in $(BIN_OUTPUTS); do \
		echo "Signing $$f"; \
		$(SIGN_TOOL) -w "$$f"; \
		$(SIGN_TOOL) -t "$$f" || true; \
	done

hw: sign

# GAL Logic (galette)
GAL_SOURCES := gal/rom.pld gal/rom_ym.pld
GAL_JEDECS := $(GAL_SOURCES:.pld=.jed)

gal: $(GAL_JEDECS)
	@command -v galette >/dev/null || { echo "Missing galette in PATH"; exit 1; }

%.jed: %.pld
	galette $<


# YM Tooling (C# Toolbox)
process-test: $(YM_TOOL)
	$(YM_TOOL) process --test ym_test_data.bin

process-stress: $(YM_TOOL)
	$(YM_TOOL) stress test1.bin

# Legacy targets for compatibility
test1.bin: process-stress
ym_test_data.bin: process-test

clean:
	rm -f $(A78_OUTPUTS) $(BIN_OUTPUTS) test1.bin ym_test_data.bin
	rm -f gal/*.jed gal/*.chp gal/*.pin gal/*.fus gal/*.pdf gal/*.sr gal/*.sim gal/*.abs
