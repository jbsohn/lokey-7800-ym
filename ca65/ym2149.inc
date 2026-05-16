; ============================================================
; YM2149.inc -- YM2149 Audio Chip Definitions (ca65)
; Custom Industrial Extension for Atari 7800
; ============================================================

; --- Hardware Registers ---
; Mapped to $4000-$4001 in the Industrial Lab layout
AY_ADDR     = $4000
AY_DATA     = $4001

; --- Constants ---
NUM_REGS    = 14

; --- Macros ---
; Simple helper to write to a YM register
.macro ym_write reg, val
    ldx #reg
    stx AY_ADDR
    lda #val
    sta AY_DATA
.endmacro
