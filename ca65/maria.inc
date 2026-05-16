; *************************************************
; *                                               *
; *   maria.inc -- MARIA EQUATES                  *
; *   Converted to ca65 from SDK Original         *
; *                                               *
; *************************************************

; *************************************************************************
; *    THIS DOCUMENT CONTAINS CONFIDENTIAL PROPRIETARY INFORMATION        *
; *    OF ATARI WHICH MAY NOT BE COPIED, DISCLOSED, OR USED EXCEPT        *
; *    AS EXPRESSLY AUTHORIZED IN WRITING BY ATARI.                       *
; *************************************************************************

;    MEMORY MAP USAGE OF THE 7800
;
;    00-1F        TIA REGISTERS
;    20-3F        MARIA REGISTERS
;    40-FF        ZERO PAGE RAM
;    100-13F      SHADOW OF TIA AND MARIA REGISTERS -- USED FOR 
;                 PUSHING ACCUMULATOR ONTO REGISTERS
;    140-1FF      RAM (STACK)
;    200-27F      NOT USED
;    280-2FF      PIA PORTS AND TIMERS
;    300-17FF     NOT USED
;    1800-203F    RAM
;    2040-20FF    SHADOW OF ZERO PAGE RAM
;    2100-213F    RAM
;    2140-21FF    SHADOW OF STACK RAM
;    2200-27FF    RAM
;    2800-3FFF    DUPLICATION OF ADDRESS SPACE 2000-27FF
;    4000-FF7F    UNUSED ADDRESS SPACE
;    FF80-FFF9    RESERVED FOR ENCRYPTION
;    FFFA-FFFF    6502 VECTORS

; --- TIA Registers ---
TIA         = $00
PTCTRL      = $01 ; INPUT PORT CONTROL ("VBLANK" IN TIA )

.ifndef INPT0
INPT0       = $08 ; PLAYER 0, RIGHT FIRE BUTTON (D7=1 WHEN PUSHED)
.endif
.ifndef INPT1
INPT1       = $09 ; PLAYER 0, LEFT FIRE BUTTON  (D7=1 WHEN PUSHED)
.endif
.ifndef INPT2
INPT2       = $0A ; PLAYER 1, RIGHT FIRE BUTTON (D7=1 WHEN PUSHED)
.endif
.ifndef INPT3
INPT3       = $0B ; PLAYER 1, LEFT FIRE BUTTON  (D7=1 WHEN PUSHED)
.endif

.ifndef INPT4
INPT4       = $0C ; PLAYER 0 FIRE BUTTON INPUT (D7=0 WHEN PUSHED)
.endif
.ifndef INPT5
INPT5       = $0D ; PLAYER 1 FIRE BUTTON INPUT (D7=0 WHEN PUSHED)
.endif

.ifndef AUDC0
AUDC0       = $15 ; AUDIO CONTROL CHANNEL 0
.endif
.ifndef AUDC1
AUDC1       = $16 ; AUDIO CONTROL CHANNEL 1
.endif
.ifndef AUDF0
AUDF0       = $17 ; AUDIO FREQUENCY CHANNEL 0
.endif
.ifndef AUDF1
AUDF1       = $18 ; AUDIO FREQUENCY CHANNEL 1
.endif
.ifndef AUDV0
AUDV0       = $19 ; AUDIO VOLUME CHANNEL 0
.endif
.ifndef AUDV1
AUDV1       = $1A ; AUDIO VOLUME CHANNEL 1
.endif

; --- MARIA Registers ---
MARIA       = $20
BKGRND      = $20 ; BACKGROUND COLOR
BACKGRND    = $20 ; synonym used by GCC
P0C1        = $21 ; PALETTE 0 - COLOR 1
P0C2        = $22
P0C3        = $23
.ifndef WSYNC
WSYNC       = $24 ; WAIT FOR SYNC
.endif
P1C1        = $25
P1C2        = $26
P1C3        = $27
MSTAT       = $28 ; MARIA STATUS
P2C1        = $29
P2C2        = $2A
P2C3        = $2B
DPPH        = $2C ; DISPLAY LIST LIST POINT HIGH BYTE
DPH         = $2C ; synonym used by GCC
P3C1        = $2D
P3C2        = $2E
P3C3        = $2F
DPPL        = $30 ; DISPLAY LIST LIST POINT LOW BYTE
DPL         = $30 ; synonym used by GCC
P4C1        = $31
P4C2        = $32
P4C3        = $33
CHBASE      = $34 ; CHARACTER BASE ADDRESS
P5C1        = $35
P5C2        = $36
P5C3        = $37
OFFSET      = $38 ; FOR FUTURE EXPANSION HERE - STORE ZER0 HERE
P6C1        = $39
P6C2        = $3A
P6C3        = $3B
CTRL        = $3C ; MARIA CONTROL REGISTER
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
R1SIZE      = ($2040-RAM1)
R2SIZE      = ($2140-RAM2)
R3SIZE      = ($2800-RAM3)

; --- System Macros ---

.macro header address, palette, width, hpos
    .byte <(address)
    .byte ((palette * $20) | ($1f & (-(width)))) & $FF
    .byte >(address)
    .byte hpos
.endmacro

.macro xheader address, palette, width, hpos, wm, ind
    .byte <(address)
    .byte ((wm * $80) | $40 | (ind * $20)) & $FF
    .byte >(address)
    .byte ((palette * $20) | ($1f & (-(width)))) & $FF
    .byte hpos
.endmacro

.macro nullhdr
    .byte 0,0
.endmacro

.macro display dli, h16, h8, offset, address
    .byte ((dli * $80) | (h16 * $40) | (h8 * $20) | offset) & $FF
    .byte >(address)
    .byte <(address)
.endmacro

.macro paint palnum, colornum, color, lum
    lda #((color * $10) | lum) & $FF
    sta BKGRND | ((palnum * 4) | colornum)
.endmacro

; screen setup bits
ckoff   = $0 ; normal color
ckon    = $1 ; kill the color
dmaoff  = $3 ; turn off dma
dmaon   = $2 ; normal dma
char1b  = $0 ; one byte character definition
char2b  = $1 ; two byte character definition
bcoff   = $0 ; black border
bcback  = $1 ; background colored border
kangoff = $0 ; transparency
kangon  = $1 ; "kangaroo" mode
mode1   = $0 ; 160x2 or 160x4
modebd  = $2 ; 320b or 320d
modeac  = $3 ; 320a or 320c

.macro screen ck, dma, cw, bc, km, mode
    lda #((ck * $80) | (dma * $20) | (cw * $10) | (bc * 8) | (km * 4) | mode) & $FF
    sta CTRL
    .ifdef sctrl
    sta sctrl
    .endif
.endmacro

.macro dppload adr
    lda #<(adr)
    sta DPPL
    .ifdef sdppl
    sta sdppl
    .endif
    lda #>(adr)
    sta DPPH
    .ifdef sdpph
    sta sdpph
    .endif
.endmacro
