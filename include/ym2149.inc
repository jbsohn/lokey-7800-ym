; ============================================================
; ym2149.inc -- YM2149 PSG
; Compatible with DASM and MADS
; ============================================================

; --- Hardware Registers ---
; Mapped to $0800-$0801 (the "YM $800" / Pokey800-compatible layout)
AY_ADDR     = $0800
AY_DATA     = $0801

; --- Constants ---
NUM_REGS    = 14
AY_MIXER    = 7
