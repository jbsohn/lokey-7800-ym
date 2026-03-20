        processor 6502

; -------------------------
; YM2149 Hardware Setup
; -------------------------
ay_addr = $4000
ay_data = $4001
background = $0020
heartbeat = $80
note_index = $81
frame_div = $82
vbi_work_div = $84
dummy_read = $2000

; Timing: 7800 PHI2 ~= 1.79MHz
; We use a manual delay for 100% startup reliability on breadboards
vbi_work_every = 4
speed_frames = 12
heartbeat_colors = 16

; Notes for 1.79MHz clock
note_c_lo = <428
note_c_hi = >428
note_d_lo = <381
note_d_hi = >381
note_e_lo = <339
note_e_hi = >339
note_f_lo = <320
note_f_hi = >320
note_g_lo = <285
note_g_hi = >285
note_a_lo = <254
note_a_hi = >254

        ; -------------------------
        ; Header Guard
        ; -------------------------
        ifnconst build_with_header
build_with_header SET 1
        endif

        org $8000

        if build_with_header
            include "a78_ym2149_header.asm"
        endif

reset:
        sei
        cld
        ldx #$ff
        txs

        ; Power-on Delay
        ldx #$00
        ldy #$00
p_1:    dex
        bne p_1
        dey
        bne p_1

        ; Clear Sound Chip (Twice)
        lda #$02
        sta heartbeat
c_pass:
        ldx #13
cl_regs:
        txa
        pha
        ldy #$00
        jsr write_ay
        pla
        tax
        dex
        bpl cl_regs
        dec heartbeat
        bne c_pass

        lda #$00
        sta heartbeat
        sta note_index
        sta frame_div

main_loop:
        jsr vbi_tick
        jsr delay_fallback
        jmp main_loop

vbi_tick:
        inc vbi_work_div
        lda vbi_work_div
        cmp #vbi_work_every
        bcc vbi_done
        lda #$00
        sta vbi_work_div

        jsr update_heartbeat

        inc frame_div
        lda frame_div
        cmp #speed_frames
        bcc vbi_done
        lda #$00
        sta frame_div

        ldx note_index
        inx
        cpx #$08
        bcc n_ok
        ldx #$00
n_ok:   stx note_index

        ; Load Note (A4 Tuning)
        lda #$00
        ldy note_table_lo,x
        jsr write_ay
        lda #$01
        ldy note_table_hi,x
        jsr write_ay
        lda #$08
        ldy #$0f
        jsr write_ay
        lda #$07
        ldy #%00111110
        jsr write_ay

vbi_done:
        rts

write_ay:
        ; Quad-Tap Address Latch
        sta ay_addr
        sta ay_addr
        sta ay_addr
        sta ay_addr
        
        ; Quad-Tap Data Write
        sty ay_data
        sty ay_data
        sty ay_data
        sty ay_data
        rts

update_heartbeat:
        inc heartbeat
        lda heartbeat
        and #$0f
        tax
        lda heartbeat_table,x
        sta background
        rts

delay_fallback:
        ldy #$12
df_1:   ldx #$00
df_2:   dex
        bne df_2
        dey
        bne df_1
        rts

note_table_lo:
        .byte note_c_lo, note_c_lo, note_g_lo, note_g_lo, note_a_lo, note_a_lo, note_g_lo, note_g_lo
note_table_hi:
        .byte note_c_hi, note_c_hi, note_g_hi, note_g_hi, note_a_hi, note_a_hi, note_g_hi, note_g_hi
heartbeat_table:
        .byte $06,$0e, $1a, $3c, $74, $38, $12, $52, $76, $28, $0c, $06, $06, $06, $06, $06

        org $fff8
        .byte $ff, $83
        org $fffa
        .word reset ; NMI
        .word reset ; RESET
        .word reset ; IRQ
