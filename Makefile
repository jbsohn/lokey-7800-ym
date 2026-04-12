# Atari 7800 YM2149 Project Makefile

# Find all 6502 assembly sources
ASM_SOURCES := $(wildcard src/ym2149_*.asm)
# Find all YM sources in src to ensure they are converted
YM_SOURCES := $(wildcard src/*.ym) $(wildcard src/*.YM)
BIN_FROM_YM := $(YM_SOURCES:.ym=.bin)
BIN_FROM_YM := $(BIN_FROM_YM:.YM=.bin)

BIN_OUTPUTS := $(ASM_SOURCES:.asm=.bin)
A78_OUTPUTS := $(ASM_SOURCES:.asm=.a78)

# Tools
SIGN_TOOL := /home/john/7800AsmDevKit/7800sign
DASM_FLAGS := -Isrc
YM2BIN     := tools/YmToBin.cs
DOTNET_SCRIPT := dotnet script
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

# Ensure YM bins are created before assembly
a78: $(BIN_FROM_YM) $(A78_OUTPUTS)

bin: $(BIN_FROM_YM) $(BIN_OUTPUTS)

# Pattern rules for DASM (keeps output in src/)
src/%.a78: src/%.asm src/a78_ym2149_header.asm
	dasm $< $(DASM_FLAGS) -f3 -o$@

src/%.bin: src/%.asm src/a78_ym2149_header.asm
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


# Specific rule for large songs to fit in 32K
src/enchant1.bin: src/enchant1.ym $(YM2BIN)
	$(DOTNET_SCRIPT) $(YM2BIN) $< -o $@ -s 2

# Pattern rule to convert any .ym file in src/ to .bin
src/%.bin: src/%.ym $(YM2BIN)
	$(DOTNET_SCRIPT) $(YM2BIN) $< -o $@

src/%.bin: src/%.YM $(YM2BIN)
	$(DOTNET_SCRIPT) $(YM2BIN) $< -o $@

clean:
	rm -f src/*.a78 src/*.bin src/*.yminc src/*.inc
	rm -f samples/*.bin samples/*.yminc samples/*.inc
	rm -f gal/*.jed gal/*.chp gal/*.pin gal/*.fus gal/*.pdf gal/*.sr gal/*.sim gal/*.abs
