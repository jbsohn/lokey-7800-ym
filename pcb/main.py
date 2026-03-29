import os
from dotenv import load_dotenv
from skidl import *
from skidl.pin import pin_types

# Load environment variables from .env file
load_dotenv()

# Set Symbol Directory from .env for robustness
# Default to common Mac path if not set
SYM_PATH = os.getenv('KICAD_SYMBOL_DIR', '/Applications/KiCad/KiCad.app/Contents/SharedSupport/symbols/')
if not SYM_PATH.endswith('/'):
    SYM_PATH += '/'

# Set official KiCad environment variables for SKiDL internals
os.environ['KICAD8_SYMBOL_DIR'] = SYM_PATH

# Load libraries directly from files - will raise error if not found
lib_mem = SchLib(SYM_PATH + 'Memory_EPROM.kicad_sym', tool=KICAD8)
lib_logic = SchLib(SYM_PATH + 'Logic_Programmable.kicad_sym', tool=KICAD8)
lib_gen = SchLib(SYM_PATH + 'Connector_Generic.kicad_sym', tool=KICAD8)
lib_dev = SchLib(SYM_PATH + 'Device.kicad_sym', tool=KICAD8)

# Component Instantiation
rom = lib_mem['27C256'].copy(ref='U1')
rom.footprint = 'Package_DIP:DIP-28_W15.24mm'

gal = lib_logic['GAL16V8'].copy(ref='U2')
gal.footprint = 'Package_DIP:DIP-20_W7.62mm'

cart = lib_gen['Conn_02x16_Counter_Clockwise'].copy(ref='J1')
cart.footprint = 'Connector_Generic:Conn_02x16_Counter_Clockwise'

c1 = lib_dev['C'].copy(ref='C1')
c1.value = '0.1uF'
c1.footprint = 'Capacitor_THT:C_Disc_D3.0mm_W1.6mm_P2.50mm'

c2 = lib_dev['C'].copy(ref='C2')
c2.value = '0.1uF'
c2.footprint = 'Capacitor_THT:C_Disc_D3.0mm_W1.6mm_P2.50mm'

# Power and Ground Nets
vcc = Net('VCC')
vcc += cart['13'], rom['28'], gal['20'], c1[1], c2[1]
vcc += rom['1']

gnd = Net('GND')
gnd += cart['14'], cart['30'], rom['14'], rom['22'], gal['10'], c1[2], c2[2]

# Address Bus Connections
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

# GAL Selection Logic
gal['2'] += cart['17'] # A15
rom['20'] += gal['19'] # !CE

# Data Bus Connections
rom['11'] += cart['27'] # D0
rom['12'] += cart['28'] # D1
rom['13'] += cart['29'] # D2
rom['15'] += cart['3']  # D3
rom['16'] += cart['4']  # D4
rom['17'] += cart['5']  # D5
rom['18'] += cart['6']  # D6
rom['19'] += cart['7']  # D7

generate_netlist()
generate_svg()