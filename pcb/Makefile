# Lokey 7800 YM - PCB Makefile
# Uses SKiDL to generate KiCad netlists from Python source.

VENV = venv
PYTHON = $(VENV)/bin/python
PIP = $(VENV)/bin/pip
SRC = main.py
NETLIST = main.net
SVG = main.svg
REQ = requirements.txt

.PHONY: all clean install

# Default target generates the netlist
all: $(NETLIST)

# Netlist depends on source AND the virtual environment
$(NETLIST): $(SRC) $(VENV)
	@echo "Generating PCB Netlist and SVG..."
	$(PYTHON) $(SRC)

# Automatic environment setup
$(VENV): $(REQ)
	@echo "Creating virtual environment and installing dependencies..."
	python3 -m venv $(VENV)
	$(PIP) install --upgrade pip
	$(PIP) install -r $(REQ)
	touch $(VENV)

# Manual install trigger
install: $(VENV)

clean:
	@echo "Cleaning PCB outputs..."
	rm -rf $(NETLIST) $(SVG) skidl.log skidl.erc
	@echo "Note: Virtual environment '$(VENV)' was NOT removed. Use 'rm -rf $(VENV)' to delete it."
