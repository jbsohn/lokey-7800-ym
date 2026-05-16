; ============================================================
; YM2149 Main Loop Heartbeat Test (MADS)
; ============================================================
    opt h- f+
    icl "maria.inc"
    icl "ym2149.inc"

    org $40
delay_val .ds 1

    org $8000

WriteYM
    stx AY_ADDR
    sta AY_DATA
    rts

Delay
    lda #$05
delay_a
    ldy #$00
delay_y
    ldx #$00
delay_x
    dex
    bne delay_x
    dey
    bne delay_y
    sec
    sbc #1
    bne delay_a
    rts

reset
    sei
    cld
    ldx #$ff
    txs

    ; Clear YM
    ldx #13
init_loop
    stx AY_ADDR
    lda #0
    cpx #7
    bne skip_mixer
    lda #$ff
skip_mixer
    sta AY_DATA
    dex
    bpl init_loop

    jsr Delay

main_loop
    ; Wait for VSync
v1  bit MSTAT
    bmi v1
v2  bit MSTAT
    bpl v2

    ; Heartbeat Sound
    ldx #7
    lda #%00111110
    jsr WriteYM
    ldx #8
    lda #15
    jsr WriteYM
    ldx #0
    lda #$56
    jsr WriteYM
    ldx #1
    lda #$03
    jsr WriteYM
    
    lda #$1A
    sta BKGRND
    jsr Delay
    
    ldx #0
    lda #$A6
    jsr WriteYM
    ldx #1
    lda #$02
    jsr WriteYM
    
    lda #$4A
    sta BKGRND
    jsr Delay

    ldx #0
    lda #$3B
    jsr WriteYM
    ldx #1
    lda #$02
    jsr WriteYM
    
    lda #$BA
    sta BKGRND
    jsr Delay
    
    jmp main_loop

    .align $fff8, $ff
    dta b($ff, $83)
    dta a(reset, reset, reset)
