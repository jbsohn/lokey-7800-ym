; ============================================================
; ym2149.inc -- YM2149 PSG
; Compatible with DASM and MADS
; ============================================================

; --- Hardware Registers ---
; Mapped to $4000-$4001 in the Industrial Lab layout
AY_ADDR     = $4000
AY_DATA     = $4001

; --- Constants ---
NUM_REGS    = 14
AY_MIXER    = 7
