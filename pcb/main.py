import os
from dotenv import load_dotenv

# Load environment variables from .env file BEFORE importing skidl
load_dotenv()

from skidl import *
from skidl.pin import pin_types

# Set default tool to KICAD8
set_default_tool(KICAD8)

# Tag the default circuit
default_circuit.tag = 'YM'

# Load libraries - SKiDL will search in KICAD8_SYMBOL_DIR
lib_mem = SchLib('Memory_EPROM', tool=KICAD8)
lib_logic = SchLib('Logic_Programmable', tool=KICAD8)
lib_gen = SchLib('Connector_Generic', tool=KICAD8)
lib_dev = SchLib('Device', tool=KICAD8)
lib_audio = SchLib('Audio', tool=KICAD8)
lib_74xx = SchLib('74xx', tool=KICAD8)

# Load the local A7800 library
project_dir = os.path.dirname(os.path.abspath(__file__))
lib_a78 = SchLib(os.path.join(project_dir, 'A7800.kicad_sym'), tool=KICAD8)

# Component Instantiation
rom = lib_mem['27C256'].copy(ref='U1', tag='YM')
rom.footprint = 'Package_DIP:DIP-28_W15.24mm'

gal = lib_logic['GAL16V8'].copy(ref='U2', tag='YM')
gal.footprint = 'Package_DIP:DIP-20_W7.62mm'

latch = lib_74xx['74HCT373'].copy(ref='U3', tag='YM')
latch.footprint = 'Package_DIP:DIP-20_W7.62mm'

ym = lib_audio['YM2149'].copy(ref='U4', tag='YM')
ym.footprint = 'Package_DIP:DIP-40_W15.24mm'

# Atari 7800 Gold Standard Cartridge Connector
# Symbol uses canonical 1-32 numbering with descriptive names.
# Footprint 'A7800' handles the 36-pin spacing and notches.
cart = lib_a78['7800edgeconn'].copy(ref='J1', tag='YM')
cart.footprint = 'Connector_PCBEdge:A7800'

# Decoupling Capacitors
def add_cap(ref):
    c = lib_dev['C'].copy(ref=ref, tag='YM')
    c.value = '0.1uF'
    c.footprint = 'Capacitor_THT:C_Disc_D3.0mm_W1.6mm_P2.50mm'
    return c

c1 = add_cap('C1'); c2 = add_cap('C2'); c3 = add_cap('C3'); c4 = add_cap('C4')

# Power and Ground Nets
vcc = Net('VCC', tag='YM')
vcc += cart['13'], rom['28'], gal['20'], latch['20'], ym['40'], ym['25'], ym['28'], ym['23']
vcc += rom['1'] # VPP tied to VCC for read-only
vcc += c1['1'], c2['1'], c3['1'], c4['1']

gnd = Net('GND', tag='YM')
gnd += cart['14'], cart['30'], rom['14'], rom['22'], gal['10'], latch['10'], latch['1'], ym['1'], ym['24']
for c in [c1, c2, c3, c4]:
    gnd += c['2']

# Address Bus Connections (Mappings follow canonical 32-pin layout)
rom['10'] += cart['26'] # A0
rom['9']  += cart['25'] # A1
rom['8']  += cart['24'] # A2
rom['7']  += cart['23'] # A3
rom['6']  += cart['22'] # A4
rom['5']  += cart['21'] # A5
rom['4']  += cart['20'] # A6
rom['3']  += cart['19'] # A7
rom['25'] += cart['12'] # A8
rom['24'] += cart['11'] # A9
rom['21'] += cart['9']  # A10
rom['23'] += cart['10'] # A11
rom['2']  += cart['8']  # A12
rom['26'] += cart['15'] # A13
rom['27'] += cart['16'] # A14

# Data Bus Connections
d0 = Net('D0', tag='YM'); d0 += cart['27'], rom['11'], latch['3']
d1 = Net('D1', tag='YM'); d1 += cart['28'], rom['12'], latch['4']
d2 = Net('D2', tag='YM'); d2 += cart['29'], rom['13'], latch['7']
d3 = Net('D3', tag='YM'); d3 += cart['3'],  rom['15'], latch['8']
d4 = Net('D4', tag='YM'); d4 += cart['4'],  rom['16'], latch['13']
d5 = Net('D5', tag='YM'); d5 += cart['5'],  rom['17'], latch['14']
d6 = Net('D6', tag='YM'); d6 += cart['6'],  rom['18'], latch['17']
d7 = Net('D7', tag='YM'); d7 += cart['7'],  rom['19'], latch['18']

# YM2149 Latched Bus
Net('DA0', tag='YM').connect(ym['37'], latch['2'])
Net('DA1', tag='YM').connect(ym['36'], latch['5'])
Net('DA2', tag='YM').connect(ym['35'], latch['6'])
Net('DA3', tag='YM').connect(ym['34'], latch['9'])
Net('DA4', tag='YM').connect(ym['33'], latch['12'])
Net('DA5', tag='YM').connect(ym['32'], latch['15'])
Net('DA6', tag='YM').connect(ym['31'], latch['16'])
Net('DA7', tag='YM').connect(ym['30'], latch['19'])

# GAL Selection Logic & Controls
gal['2'] += cart['17'] # A15
gal['3'] += cart['16'] # A14
gal['4'] += cart['26'] # A0
gal['5'] += cart['2']  # HALT
gal['6'] += cart['1']  # RW
gal['7'] += cart['32'] # Phase_2_CLK

# Control nets
Net('ROM_CE', tag='YM').connect(rom['20'], gal['19'])
Net('YM_LE', tag='YM').connect(latch['11'], gal['15'])
Net('PHI2OUT', tag='YM').connect(ym['22'], gal['16'])
Net('BC1', tag='YM').connect(ym['29'], gal['17'])
Net('BDIR', tag='YM').connect(ym['27'], gal['18'])

generate_netlist()
generate_svg()
