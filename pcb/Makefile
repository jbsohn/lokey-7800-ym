# Lokey 7800 YM - PCB Makefile
# Uses SKiDL to generate KiCad netlists from Python source.

PYTHON = ./venv/bin/python
SRC = main.py
NETLIST = main.net
SVG = main.svg

.PHONY: all clean

all: $(NETLIST)

$(NETLIST): $(SRC)
	@echo "Generating PCB Netlist and SVG..."
	$(PYTHON) $(SRC)

clean:
	@echo "Cleaning PCB outputs..."
	rm -f $(NETLIST) $(SVG) skidl.log skidl.erc
