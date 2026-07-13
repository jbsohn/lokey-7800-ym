; ============================================================
; bank.asm -- 32-pin board YM-IOA bank select test:
; a chromatic scale, one note per bank, using every free bank.
;
; Each of the 14 banks (0-13) holds a 2-byte YM2149 tone
; (fine, coarse) for one chromatic semitone starting at middle C
; (C4 up to C#5). The fixed-bank code selects a bank via the YM's
; IOA port, loads that bank's into channel A's tone
; registers, holds it for 1 second, advances to the next bank, and
; wraps back to bank 0 after C#5 -- a continuous, repeating run.
; ============================================================

    processor 6502
    include "maria.inc"
    include "ym2149.inc"

BANK_SIZE        = $4000
NUM_NOTES        = 14          ; C4 C#4 D4 D#4 E4 F4 F#4 G4 G#4 A4 A#4 B4 C5 C#5
NOTE_HOLD_FRAMES = 60          ; 1s per note at 60Hz NTSC

bank_num = $80                 ; zero page

    MAC NOTE_BANK
        ; {1}/{2} = fine/coarse tune, stored at the start of the bank.
        RORG $4000
        .byte {1}, {2}
        .ds BANK_SIZE-2, 0
    ENDM

    ORG $0000

    ; 1789772.5 / (16 * freq); values rounded to nearest int.
    NOTE_BANK $AC, $01      ; bank  0: C4   428 -> 261.36 Hz (target 261.63)
    NOTE_BANK $94, $01      ; bank  1: C#4  404 -> 276.88 Hz (target 277.18)
    NOTE_BANK $7D, $01      ; bank  2: D4   381 -> 293.60 Hz (target 293.66)
    NOTE_BANK $68, $01      ; bank  3: D#4  360 -> 310.72 Hz (target 311.13)
    NOTE_BANK $53, $01      ; bank  4: E4   339 -> 329.97 Hz (target 329.63)
    NOTE_BANK $40, $01      ; bank  5: F4   320 -> 349.56 Hz (target 349.23)
    NOTE_BANK $2E, $01      ; bank  6: F#4  302 -> 370.40 Hz (target 369.99)
    NOTE_BANK $1D, $01      ; bank  7: G4   285 -> 392.49 Hz (target 391.10)
    NOTE_BANK $0D, $01      ; bank  8: G#4  269 -> 415.84 Hz (target 415.30)
    NOTE_BANK $FE, $00      ; bank  9: A4   254 -> 440.40 Hz (target 440.00)
    NOTE_BANK $F0, $00      ; bank 10: A#4  240 -> 466.09 Hz (target 466.16)
    NOTE_BANK $E2, $00      ; bank 11: B4   226 -> 494.96 Hz (target 493.88)
    NOTE_BANK $D6, $00      ; bank 12: C5   214 -> 522.71 Hz (target 523.25)
    NOTE_BANK $CA, $00      ; bank 13: C#5  202 -> 553.77 Hz (target 554.37)

    RORG $8000

reset:
        sei
        cld
        ldx #$ff
        txs

        ldx #NUM_REGS-1
init_loop:
        stx AY_ADDR
        lda #0
        sta AY_DATA
        dex
        bpl init_loop

        ; Mixer: channel A tone only (bit0=0 enables it), noise and
        ; channels B/C off (bits 1-5=1), OR'd with AY_IOA_OUTPUT
        ; (register 7 discipline, docs/Hardware-32pin.md).
        lda #AY_MIXER
        sta AY_ADDR
        lda #(AY_IOA_OUTPUT | %00111110)
        sta AY_DATA

        ; Channel A amplitude, fixed volume (no envelope).
        lda #8
        sta AY_ADDR
        lda #15
        sta AY_DATA

        lda #0
        sta bank_num

note_loop:
        lda #AY_IO_A             ; register 14 = IO Port A (bank number)
        sta AY_ADDR
        lda bank_num
        sta AY_DATA

        lda #0                  ; channel A fine tune <- bank's byte 0
        sta AY_ADDR
        lda $4000
        sta AY_DATA

        lda #1                  ; channel A coarse tune <- bank's byte 1
        sta AY_ADDR
        lda $4001
        sta AY_DATA

        lda bank_num             ; visual cue synced with the note
        asl
        asl
        asl
        asl
        sta BKGRND

        ldy #NOTE_HOLD_FRAMES
hold_loop:
        jsr sync_vbi
        dey
        bne hold_loop

        inc bank_num
        lda bank_num
        cmp #NUM_NOTES
        bne note_loop
        lda #0
        sta bank_num
        jmp note_loop

sync_vbi:
v1:     bit MSTAT
        bmi v1
v2:     bit MSTAT
        bpl v2
        rts

        ; -------------------------
        ; Interrupt Vectors
        ; -------------------------
        .ds ($fff8 - .), $ff
        .byte $ff, $83
        .word reset
        .word reset
        .word reset
