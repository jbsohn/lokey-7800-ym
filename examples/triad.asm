; ============================================================
; triad.asm -- YM2149 Main Loop Triad Test (Universal)
; ============================================================

    .if MADS
      opt h- f+
      icl "maria.inc"
      icl "ym2149.inc"
      blk none $8000
    .else
      processor 6502
      include "maria.inc"
      include "ym2149.inc"
      org $8000
    .endif

reset:
        sei
        cld
        ldx #$ff
        txs

        ; -------------------------
        ; YM2149 Initial "Crush"
        ; -------------------------
        ldx #NUM_REGS-1
init_loop:
        txa
        ldy #0
        cpx #AY_MIXER
        bne skip_init_mixer
        ldy #$ff
skip_init_mixer:
        jsr write_ay
        dex
        bpl init_loop

        jsr sound_delay

main_loop:
v1:     bit MSTAT
        bmi v1
v2:     bit MSTAT
        bpl v2

        lda #AY_MIXER
        ldy #%00111110
        jsr write_ay
        lda #8
        ldy #15
        jsr write_ay

        lda #0
        ldy #$56
        jsr write_ay
        lda #1
        ldy #$03
        jsr write_ay
        
        lda #$1A
        sta BKGRND
        jsr sound_delay
        
        lda #0
        ldy #$A6
        jsr write_ay
        lda #1
        ldy #$02
        jsr write_ay
        
        lda #$4A
        sta BKGRND
        jsr sound_delay

        lda #0
        ldy #$3B
        jsr write_ay
        lda #1
        ldy #$02
        jsr write_ay
        
        lda #$BA
        sta BKGRND
        jsr sound_delay
        
        jmp main_loop

write_ay:
        sta AY_ADDR
        sty AY_DATA
        rts

sound_delay:
        lda #$05
delay_a:
        ldy #$00
delay_y:
        ldx #$00
delay_x:
        dex
        bne delay_x
        dey
        bne delay_y
        sec
        sbc #1
        bne delay_a
        rts

        ; -------------------------
        ; Interrupt Vectors
        ; -------------------------
    .if MADS
        .align $fff8, $ff
        .byte $ff, $83
        .word reset, reset, reset
    .else
        org $fff8, $ff
        .byte $ff, $83
        .word reset
        .word reset
        .word reset
    .endif
