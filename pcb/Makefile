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
	@echo "Applying GitHub-friendly optimizations to SVG..."
	@# 1. Apply white background for Dark Mode compatibility
	@sed -i '' 's/<svg /<svg style="background-color:white" /' $(SVG)
	@sed -i '' 's/<svg[^>]*>/&<rect width="100%" height="100%" fill="white"\/>/' $(SVG)
	@# 2. Add viewBox and set width to 100% for responsive rendering in Markdown
	@# Extracts current width/height to build the viewBox
	@W=$$(grep -o 'width="[^"]*"' $(SVG) | head -1 | cut -d'"' -f2); \
	 H=$$(grep -o 'height="[^"]*"' $(SVG) | head -1 | cut -d'"' -f2); \
	 sed -i '' "s/<svg /<svg viewBox=\"0 0 $$W $$H\" /" $(SVG); \
	 sed -i '' 's/width="[^"]*"/width="100%"/' $(SVG); \
	 sed -i '' 's/height="[^"]*"/height="auto"/' $(SVG)

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
	rm -f $(NETLIST) $(SVG) skidl.log skidl.erc
	@echo "Note: Virtual environment '$(VENV)' was NOT removed. Use 'rm -rf $(VENV)' to delete it."
