; *************************************************
; *                                               *
; *   maria.inc -- MARIA EQUATES (MADS)           *
; *                                               *
; *************************************************

; --- TIA Registers ---
TIA         = $00
PTCTRL      = $01

.ifndef INPT0
INPT0       = $08
.endif
.ifndef INPT1
INPT1       = $09
.endif
.ifndef INPT2
INPT2       = $0A
.endif
.ifndef INPT3
INPT3       = $0B
.endif

.ifndef INPT4
INPT4       = $0C
.endif
.ifndef INPT5
INPT5       = $0D
.endif

.ifndef AUDC0
AUDC0       = $15
.endif
.ifndef AUDC1
AUDC1       = $16
.endif
.ifndef AUDF0
AUDF0       = $17
.endif
.ifndef AUDF1
AUDF1       = $18
.endif
.ifndef AUDV0
AUDV0       = $19
.endif
.ifndef AUDV1
AUDV1       = $1A
.endif

; --- MARIA Registers ---
MARIA       = $20
BKGRND      = $20
BACKGRND    = $20
P0C1        = $21
P0C2        = $22
P0C3        = $23
.ifndef WSYNC
WSYNC       = $24
.endif
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
R1SIZE      = ($2040-RAM1)
R2SIZE      = ($2140-RAM2)
R3SIZE      = ($2800-RAM3)

; --- System Macros ---

; header address, palette, width, hpos
.macro m_header
    dta b(<:1)
    dta b(((:2 * $20) | ($1f & (-:3))) & $FF)
    dta b(>:1)
    dta b(:4)
.endm

; xheader address, palette, width, hpos, wm, ind
.macro m_xheader
    dta b(<:1)
    dta b(((:5 * $80) | $40 | (:6 * $20)) & $FF)
    dta b(>:1)
    dta b(((:2 * $20) | ($1f & (-:3))) & $FF)
    dta b(:4)
.endm

.macro m_nullhdr
    dta b(0,0)
.endm

; display dli, h16, h8, offset, address
.macro m_display
    dta b(((:1 * $80) | (:2 * $40) | (:3 * $20) | :4) & $FF)
    dta b(>:5)
    dta b(<:5)
.endm

; paint palnum, colornum, color, lum
.macro m_paint
    lda #((:3 * $10) | :4) & $FF
    sta BKGRND | ((:1 * 4) | :2)
.endm

; screen ck, dma, cw, bc, km, mode
.macro m_screen
    lda #((:1 * $80) | (:2 * $20) | (:3 * $10) | (:4 * 8) | (:5 * 4) | :6) & $FF
    sta CTRL
.endm

.macro m_dppload
    lda #<:1
    sta DPPL
    lda #>:1
    sta DPPH
.endm
