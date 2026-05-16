; ============================================================
; YM2149 Melody VBI Test (ca65)
; ============================================================
    .setcpu "6502X"
    
    
    .include "maria.inc"
    .include "ym2149.inc"

; Constants
NOTE_C = 428
NOTE_G = 285
NOTE_A = 254

.segment "ZEROPAGE"
heartbeat:    .res 1
note_index:   .res 1
frame_div:    .res 1
vbi_work_div: .res 1

.segment "CODE"

.proc reset
    sei
    cld
    ldx #$ff
    txs

    ; Power delay
    ldx #0
    ldy #0
p1: dex
    bne p1
    dey
    bne p1

    ; Clear YM
    ldx #13
cl_y:
    stx AY_ADDR
    lda #0
    sta AY_DATA
    dex
    bpl cl_y

    lda #0
    sta heartbeat
    sta note_index
    sta frame_div
    sta vbi_work_div

main_loop:
    jsr Tick
    jsr delay_fallback
    jmp main_loop
.endproc

.proc Tick
    VBI_WORK_EVERY   = 4
    SPEED_FRAMES     = 12

    inc vbi_work_div
    lda vbi_work_div
    cmp #VBI_WORK_EVERY
    bcc vbi_done
    lda #0
    sta vbi_work_div

    jsr UpdateHeartbeat

    inc frame_div
    lda frame_div
    cmp #SPEED_FRAMES
    bcc vbi_done
    lda #0
    sta frame_div

    ldx note_index
    inx
    cpx #8
    bcc n_ok
    ldx #0
n_ok:
    stx note_index

    ldy note_index
    ldx #0
    lda note_table_lo,y
    jsr WriteYM
    ldx #1
    lda note_table_hi,y
    jsr WriteYM
    ldx #8
    lda #$0F
    jsr WriteYM
    ldx #7
    lda #%00111110
    jsr WriteYM
vbi_done:
    rts
.endproc

.proc WriteYM
    stx AY_ADDR
    sta AY_DATA
    rts
.endproc

.proc delay_fallback
    ldy #$12
df_1:
    ldx #$00
df_2:
    dex
    bne df_2
    dey
    bne df_1
    rts
.endproc

.proc UpdateHeartbeat
    inc heartbeat
    lda heartbeat
    and #$0f
    tax
    lda heartbeat_table,x
    sta BACKGRND
    rts
.endproc

note_table_lo: .byte <NOTE_C, <NOTE_C, <NOTE_G, <NOTE_G, <NOTE_A, <NOTE_A, <NOTE_G, <NOTE_G
note_table_hi: .byte >NOTE_C, >NOTE_C, >NOTE_G, >NOTE_G, >NOTE_A, >NOTE_A, >NOTE_G, >NOTE_G
heartbeat_table: .byte $06, $0e, $1a, $3c, $74, $38, $12, $52, $76, $28, $0c, $06, $06, $06, $06, $06

.segment "VECTORS"
    .word reset ; NMI
    .word reset ; RESET
    .word reset ; IRQ
