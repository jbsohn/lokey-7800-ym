        processor 6502

; Minimal Atari 7800 YM2149 test:
; - Initialize stack
; - Enable tone on channel A only
; - Set fixed pitch + fixed volume
; - Loop forever

ay_addr = $4000
ay_data = $4001
background = $0020
heartbeat = $80

; A4 ~= 440 Hz for YM clock ~= 1.789772 MHz:
; period ~= 1,789,772 / (16 * 440) ~= 254
tone_a4_lo = <254
tone_a4_hi = >254

        org $8000

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

        ; R0/R1 = channel A tone period
        lda #$00
        sta ay_addr
        lda #tone_a4_lo
        sta ay_data

        lda #$01
        sta ay_addr
        lda #tone_a4_hi
        sta ay_data

        ; R7 mixer: tone A on, tone B/C off, noise off
        lda #$07
        sta ay_addr
        lda #%00111110
        sta ay_data

        ; R8 = channel A fixed volume max
        lda #$08
        sta ay_addr
        lda #$0f
        sta ay_data

forever:
        lda heartbeat
        clc
        adc #$22
        sta heartbeat
        ora #$06
        sta background
        jsr delay
        jmp forever

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

        org $fff8
        .byte $ff
        .byte $83
        org $fffa
        .word reset
        .word reset
        .word reset
