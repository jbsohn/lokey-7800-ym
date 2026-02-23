        processor 6502

; -------------------------
; YM2149 write-only ports
; -------------------------
ay_addr = $4000
ay_data = $4001
background = $0020
heartbeat = $80
tempo_count = $81
speed_mult = 4

; -------------------------
; Notes (YM2149 tone period = f_clk / (16 * f_out))
; f_clk ≈ 1.789772 MHz
; C4 ~ 261.6 Hz -> period ≈ 1,789,772 / (16*261.6) ≈ 427
; E4 ~ 329.6 Hz -> period ≈ 339
; G4 ~ 392.0 Hz -> period ≈ 285
; -------------------------
note_c_lo = <427
note_c_hi = >427
note_e_lo = <339
note_e_hi = >339
note_g_lo = <285
note_g_hi = >285

        org $8000

        ; -------------------------
        ; A78 HEADER (v4)
        ; -------------------------
        ifnconst build_with_header
build_with_header SET 1
        endif

        if build_with_header
                include "a78_ym2149_header.asm"
        endif

reset:
        sei
        cld
        ldx #$ff
        txs

        lda #$00
        sta heartbeat
        ora #$06
        sta background

        ; -------------------------
        ; YM2149 init
        ; -------------------------
        ; Disable noise, enable tone A only
        lda #$07
        sta ay_addr
        lda #%00111110
        sta ay_data

        ; Set volume A to max (fixed volume)
        lda #$08
        sta ay_addr
        lda #$0f
        sta ay_data

main_loop:
        ; C4
        jsr tick_vis
        lda #$00
        sta ay_addr
        lda #note_c_lo
        sta ay_data
        lda #$01
        sta ay_addr
        lda #note_c_hi
        sta ay_data
        jsr delay_tempo

        ; E4
        jsr tick_vis
        lda #$00
        sta ay_addr
        lda #note_e_lo
        sta ay_data
        lda #$01
        sta ay_addr
        lda #note_e_hi
        sta ay_data
        jsr delay_tempo

        ; G4
        jsr tick_vis
        lda #$00
        sta ay_addr
        lda #note_g_lo
        sta ay_data
        lda #$01
        sta ay_addr
        lda #note_g_hi
        sta ay_data
        jsr delay_tempo

        jmp main_loop

; -------------------------
; Visual heartbeat
; -------------------------
tick_vis:
        lda heartbeat
        clc
        adc #$22
        sta heartbeat
        ora #$06
        sta background
        rts

; -------------------------
; Delay loop
; -------------------------
delay:
        ldy #$60
delay_y:
        ldx #$ff
delay_x:
        dex
        bne delay_x
        dey
        bne delay_y
        rts

delay_tempo:
        lda #speed_mult
        sta tempo_count
delay_tempo_loop:
        jsr delay
        dec tempo_count
        bne delay_tempo_loop
        rts

        org $fff8
        .byte $ff         ; required by 7800sign
        .byte $83         ; ROM start $8000
        org $fffa
        .word reset
        .word reset
        .word reset
