; ============================================================
; YM2149 Main Loop Heartbeat Test (ca65)
; ============================================================
    .setcpu "6502X"
    
    .include "maria.inc"
    .include "ym2149.inc"

.segment "ZEROPAGE"
delay_val:  .res 1

.segment "CODE"

.proc WriteYM
    stx AY_ADDR
    sta AY_DATA
    rts
.endproc

.proc Delay
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
.endproc

.proc reset
    sei
    cld
    ldx #$ff
    txs

    ; Clear YM
    ldx #13
init_loop:
    stx AY_ADDR
    lda #0
    cpx #7
    bne skip_mixer
    lda #$ff
skip_mixer:
    sta AY_DATA
    dex
    bpl init_loop

    jsr Delay

main_loop:
    ; Wait for VSync for consistent speed
v1: bit MSTAT
    bmi v1
v2: bit MSTAT
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
.endproc

.segment "VECTORS"
    .word reset ; NMI
    .word reset ; RESET
    .word reset ; IRQ
