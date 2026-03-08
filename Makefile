ASM_SOURCES := $(wildcard ym2149_*.asm)
A78_OUTPUTS := $(ASM_SOURCES:.asm=.a78)
BIN_OUTPUTS := $(ASM_SOURCES:.asm=.bin)

SIGN_TOOL ?= /home/john/7800AsmDevKit/7800sign
# using WinCupl in VM for now
# GAL_SOURCE ?= gal/ym4000.pld
DASM_FLAGS ?=

.PHONY: all help a78 bin sign hw gal clean

all: a78

help:
	@echo "Targets:"
	@echo "  make a78   - Build .a78 images for all ym2149_*.asm sources"
	@echo "  make bin   - Build raw .bin images (no A78 header)"
	@echo "  make sign  - Build .bin images and sign them with 7800sign"
	@echo "  make hw    - Alias for sign (hardware-ready .bin files)"
	@echo "  make gal   - Build JEDEC from $(GAL_SOURCE) using galette"
	@echo "  make clean - Remove generated .a78/.bin/.jed files"

a78: $(A78_OUTPUTS)

bin: $(BIN_OUTPUTS)

%.a78: %.asm
	dasm $< $(DASM_FLAGS) -f3 -o$@

%.bin: %.asm
	dasm $< $(DASM_FLAGS) -Dbuild_with_header=0 -f3 -o$@

sign: bin
	@command -v "$(SIGN_TOOL)" >/dev/null || { echo "Missing 7800sign at $(SIGN_TOOL)"; exit 1; }
	@for f in $(BIN_OUTPUTS); do \
		echo "Signing $$f"; \
		"$(SIGN_TOOL)" -w "$$f"; \
		"$(SIGN_TOOL)" -t "$$f" || true; \
	done

hw: sign

# using WinCupl in VM for now
# gal:
# 	@command -v galette >/dev/null || { echo "Missing galette in PATH"; exit 1; }
# 	galette $(GAL_SOURCE)

clean:
	rm -f $(A78_OUTPUTS) $(BIN_OUTPUTS) gal/*.jed gal/*.chp gal/*.pin gal/*.fus gal/*.pdf gal/*.sr gal/*.sim gal/*.abs

