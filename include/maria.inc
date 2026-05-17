; *************************************************
; *                                               *
; *   maria.inc -- MARIA EQUATES (Universal)      *
; *   Compatible with DASM and MADS               *
; *                                               *
; *************************************************

; --- TIA Registers ---
TIA         = $00
PTCTRL      = $01

INPT0       = $08
INPT1       = $09
INPT2       = $0A
INPT3       = $0B
INPT4       = $0C
INPT5       = $0D

AUDC0       = $15
AUDC1       = $16
AUDF0       = $17
AUDF1       = $18
AUDV0       = $19
AUDV1       = $1A

; --- MARIA Registers ---
MARIA       = $20
BKGRND      = $20
BACKGRND    = $20
P0C1        = $21
P0C2        = $22
P0C3        = $23
WSYNC       = $24
P1C1        = $25
P1C2        = $26
P1C3        = $27
MSTAT       = $28
P2C1        = $29
P2C2        = $2A
P2C3        = $2B
DPPH        = $2C
DPH         = $2C
P3C1        = $2D
P3C2        = $2E
P3C3        = $2F
DPPL        = $30
DPL         = $30
P4C1        = $31
P4C2        = $32
P4C3        = $33
CHBASE      = $34
P5C1        = $35
P5C2        = $36
P5C3        = $37
OFFSET      = $38
P6C1        = $39
P6C2        = $3A
P6C3        = $3B
CTRL        = $3C
P7C1        = $3D
P7C2        = $3E
P7C3        = $3F

; --- PIA and TIMER (6532) ---
SWCHA       = $0280
CTLSWA      = $0281
SWCHB       = $0282
CTLSWB      = $0283
INTIM       = $0284
TIM1T       = $0294
TIM8T       = $0295
TIM64T      = $0296
T1024T      = $0297

; --- Constants ---
RESET       = 1
SELECT      = 2
PAUSE       = 8

RAM1        = $1800
RAM2        = $2100
RAM3        = $2200

; --- Helper Macros (Optional) ---
; Note: These are defined separately for each assembler if needed.
