; ============================================================
; triad_mads.asm -- MADS "Pro" Triad Demo
; ============================================================
; WHY USE MADS?
; This version uses MADS "Pro" Procedures to make the YM2149 
; driver logic look like modern function calls.
;
; KEY FEATURES SHOWN:
; 1. .PROC   - Encapsulated procedures (functions).
; 2. .REG    - Automatic register-based parameter passing.
; 3. MVA     - Replaces LDA/STA pairs for better readability.
; ============================================================

    opt h- f+        ; MADS: No DOS header, raw binary
    icl "maria.inc"
    icl "ym2149.inc"
    blk none $8000

; ------------------------------------------------------------
; 1. Zero Page allocation
; ------------------------------------------------------------
.zpvar temp_val .byte = $40

; ------------------------------------------------------------
; 2. Procedures with Parameters
; ------------------------------------------------------------

; WriteYM: Pass Register in A, Value in Y
.proc WriteYM (.byte a, y) .reg
    sta AY_ADDR
    sty AY_DATA
    rts
.endp

; Delay: Pass number of outer loops in A
.proc Delay (.byte a) .reg
loop_a:
    ldy #$00
loop_y:
    ldx #$00
loop_x:
    dex
    bne loop_x
    dey
    bne loop_y
    sec
    sbc #1
    bne loop_a
    rts
.endp

; ------------------------------------------------------------
; 3. Main Logic
; ------------------------------------------------------------
reset:
    sei
    cld
    ldx #$ff
    txs

    ; Clear YM using a loop and our new procedure
    ldx #13
init_loop:
    txa
    ldy #0
    cpx #7
    #if .byte @ == #7
        ldy #$ff
    #end
    jsr WriteYM
    dex
    bpl init_loop

    ; Call Delay procedure with parameter in A
    lda #5
    jsr Delay

main_loop:
    v1: bit MSTAT
        bmi v1
    v2: bit MSTAT
        bpl v2

    ; 3. CLEAN FUNCTION-STYLE CALLS:
    WriteYM #7, #%00111110
    WriteYM #8, #15

    ; Beat 1: Gold
    WriteYM #0, #$56
    WriteYM #1, #$03
    mva #$1A BKGRND
    lda #5
    jsr Delay
    
    ; Beat 2: Red
    WriteYM #0, #$A6
    WriteYM #1, #$02
    mva #$4A BKGRND
    lda #5
    jsr Delay

    ; Beat 3: Blue
    WriteYM #0, #$3B
    WriteYM #1, #$02
    mva #$BA BKGRND
    lda #5
    jsr Delay
    
    jmp main_loop

; ------------------------------------------------------------
; 4. ROM PADDING
; ------------------------------------------------------------
    .align $fff8, $ff
    .byte $ff, $83
    .word reset, reset, reset
