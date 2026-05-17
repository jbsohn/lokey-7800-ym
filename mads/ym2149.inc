; ============================================================
; YM2149.inc -- YM2149 Audio Chip Definitions (MADS)
; ============================================================

AY_ADDR     = $4000
AY_DATA     = $4001

NUM_REGS    = 14

.macro m_ym_write
    ldx #:1
    stx AY_ADDR
    lda #:2
    sta AY_DATA
.endm
