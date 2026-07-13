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

; --- 32-pin board bank select (docs/Hardware-32pin.md) ---
; Bank number lives in register 14 (I/O Port A); its value only reaches
; the PLD's ROMA14-17 mapper once register 7 bit 6 puts IOA in output
; mode. Every AY_MIXER (reg 7) write must OR in AY_IOA_OUTPUT, or IOA
; reverts to input and the banked $4000-$7FFF window floats to its
; power-on pull-up state (bank 15, a mirror of the fixed region).
AY_IO_A       = 14
AY_IOA_OUTPUT = %01000000
