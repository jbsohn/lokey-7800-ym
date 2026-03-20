        processor 6502

; ============================================================
; YM2149 Main Loop Heartbeat Test
; - Visual heartbeat on background ($0020)
; - Sound toggling in main loop (outside VBI)
; ============================================================

ay_addr    = $4000
ay_data    = $4001
background = $0020
dummy_read = $2000

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

        ; -------------------------
        ; YM2149 Initial "Crush"
        ; -------------------------
        ; Sweep all 16 registers (0-15)
        ldx #15
init_loop:
        txa             ; Reg index -> A
        ldy #0          ; Default 0
        cpx #7          ; Mixer?
        bne skip_init_mixer
        ldy #$ff        ; All off
skip_init_mixer:
        jsr write_ay
        dex
        bpl init_loop

        ; Wait a bit after crush to verify silence
        jsr sound_delay

main_loop:
        ; -------------------------
        ; Setup Note Environment (Inside loop for reliability)
        ; -------------------------
        ; Enable Tone A only (Reg 7)
        lda #7
        ldy #%00111110
        jsr write_ay

        ; Set Volume A to Max (Reg 8)
        lda #8
        ldy #15
        jsr write_ay

        ; -------------------------
        ; Note 1: C3 (~131Hz)
        ; Period = 1.79e6 / (16 * 131) = 854 ($0356)
        ; -------------------------
        lda #0          ; Reg 0: Tone A Period Low
        ldy #$56
        jsr write_ay
        lda #1          ; Reg 1: Tone A Period High
        ldy #$03
        jsr write_ay
        
        lda #$1A        ; Gold color heartbeat
        sta background
        jsr sound_delay
        
        ; -------------------------
        ; Note 2: E3 (~165Hz)
        ; Period = 1.79e6 / (16 * 165) = 678 ($02A6)
        ; -------------------------
        lda #0
        ldy #$A6
        jsr write_ay
        lda #1
        ldy #$02
        jsr write_ay
        
        lda #$4A        ; Red color heartbeat
        sta background
        jsr sound_delay

        ; -------------------------
        ; Note 3: G3 (~196Hz)
        ; Period = 1.79e6 / (16 * 196) = 571 ($023B)
        ; -------------------------
        lda #0
        ldy #$3B
        jsr write_ay
        lda #1
        ldy #$02
        jsr write_ay
        
        lda #$BA        ; Blue color heartbeat
        sta background
        jsr sound_delay
        
        jmp main_loop

; -------------------------
; write_ay: Address latch then Data write
; Interleaved Quad-Write for maximum reliability
; -------------------------
write_ay:
        sta ay_addr         ; Latch Addr 1
        sty ay_data         ; Write Data 1
        sta ay_addr         ; Latch Addr 2
        sty ay_data         ; Write Data 2
        sta ay_addr         ; Latch Addr 3
        sty ay_data         ; Write Data 3
        sta ay_addr         ; Latch Addr 4
        sty ay_data         ; Write Data 4
        rts

; -------------------------
; sound_delay: 3-level loop for multi-second delays
; -------------------------
sound_delay:
        lda #$05        ; 1-second heartbeat (Diagnostic Tempo)
delay_a:
        ldy #$00        ; Middle loop
delay_y:
        ldx #$00        ; Inner loop
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
        org $fff8
        .byte $ff         ; 7800sign requirement
        .byte $83         ; ROM start $8000
        org $fffa
        .word reset
        .word reset
        .word reset
