; =========================================================
; YM2149 MVP Player for Atari 7800 (ca65 port)
; 1:1 Logic match to original stable DASM version
; =========================================================
    .include "maria.inc"
    .include "YM2149.inc"
    .include "ymb.inc"

; Imports from music data object
.import MusicData
.import MAX_FRAMES : abs
.import STEP : abs

.segment "ZEROPAGE"
    player: .res .sizeof(TPlayerState)

.segment "CODE"

; --- Entry Point ---
.proc reset
    sei
    cld
    ldx #$ff
    txs

    ; Silence Audio
    lda #0
    sta AUDV0
    sta AUDV1

    ; Power-on Delay
    ldx #0
    ldy #0
p1: dex
    bne p1
    dey
    bne p1

    ; Clear YM Registers
    ldx #NUM_REGS-1
cl_y:
    stx AY_ADDR
    lda #0
    sta AY_DATA
    dex
    bpl cl_y

    jsr init_music

main_loop:
    jsr sync_vbi
    jsr update_visuals
    
    ; 16-bit Fractional Music Step
    clc
    lda player + TPlayerState::music_acc
    adc player + TPlayerState::music_delta
    sta player + TPlayerState::music_acc
    lda player + TPlayerState::music_acc+1
    adc player + TPlayerState::music_delta+1
    sta player + TPlayerState::music_acc+1
    bcc skip
    jsr play_frame
skip:
    jmp main_loop
.endproc

; --- Music Core ---

.proc init_music
    lda #0
    ldx #.sizeof(TPlayerState)-1
cl_p: 
    sta player,x
    dex
    bpl cl_p

    ; Use pre-calculated music delta from link-time constant
    lda #<STEP
    sta player + TPlayerState::music_delta
    lda #>STEP
    sta player + TPlayerState::music_delta+1

    lda MusicData
    sta player + TPlayerState::pat_size

    clc
    lda #<MusicData
    adc #3
    sta player + TPlayerState::seq_base
    lda #>MusicData
    adc #0
    sta player + TPlayerState::seq_base+1

    ldy #2 ; seq_len offset
    clc
    lda player + TPlayerState::seq_base
    adc MusicData,y
    sta player + TPlayerState::pat_table
    lda #0
    adc player + TPlayerState::seq_base+1
    sta player + TPlayerState::pat_table+1

    ldy #1 ; num_patterns offset
    lda MusicData,y
    asl
    tay
    lda #0
    rol
    tax
    
    clc
    tya
    adc player + TPlayerState::pat_table
    sta player + TPlayerState::pat_base
    txa
    adc player + TPlayerState::pat_table+1
    sta player + TPlayerState::pat_base+1
    rts
.endproc

.proc play_frame
    lda player + TPlayerState::frame_cnt+1
    cmp #>MAX_FRAMES
    bne check_pattern
    lda player + TPlayerState::frame_cnt
    cmp #<MAX_FRAMES
    bcc check_pattern
    
    jsr init_music

check_pattern:
    lda player + TPlayerState::pat_frames
    bne do_play
    ldy player + TPlayerState::seq_idx
    lda (player + TPlayerState::seq_base),y
    inc player + TPlayerState::seq_idx
    asl
    tay
    lda (player + TPlayerState::pat_table),y
    sta player + TPlayerState::music_ptr
    iny
    lda (player + TPlayerState::pat_table),y
    sta player + TPlayerState::music_ptr+1
    clc
    lda player + TPlayerState::music_ptr
    adc player + TPlayerState::pat_base
    sta player + TPlayerState::music_ptr
    lda player + TPlayerState::music_ptr+1
    adc player + TPlayerState::pat_base+1
    sta player + TPlayerState::music_ptr+1
    lda player + TPlayerState::pat_size
    sta player + TPlayerState::pat_frames
do_play:
    dec player + TPlayerState::pat_frames
    ldy #0
    lda (player + TPlayerState::music_ptr),y
    sta player + TPlayerState::tmp_mask
    iny
    lda (player + TPlayerState::music_ptr),y
    sta player + TPlayerState::tmp_mask+1
    clc
    lda player + TPlayerState::music_ptr
    adc #2
    sta player + TPlayerState::music_ptr
    lda player + TPlayerState::music_ptr+1
    adc #0
    sta player + TPlayerState::music_ptr+1
    ldx #0
reg_loop:
    cpx #8
    bcc low
    txa
    sec
    sbc #8
    tay
    lda bit_table,y
    and player + TPlayerState::tmp_mask+1
    beq next
    bne upd
low:
    txa
    tay
    lda bit_table,y
    and player + TPlayerState::tmp_mask
    beq next
upd:
    stx AY_ADDR
    ldy #0
    lda (player + TPlayerState::music_ptr),y
    sta AY_DATA
    inc player + TPlayerState::music_ptr
    bne next
    inc player + TPlayerState::music_ptr+1
next:
    inx
    cpx #NUM_REGS
    bne reg_loop
    inc player + TPlayerState::frame_cnt
    bne p_done
    inc player + TPlayerState::frame_cnt+1
p_done:
    rts

bit_table:
    .byte $01,$02,$04,$08,$10,$20,$40,$80
.endproc

.proc sync_vbi
v1: bit MSTAT
    bmi v1     ; Wait for CURRENT vblank to end
v2: bit MSTAT
    bpl v2     ; Wait for NEXT vblank to start

    inc player + TPlayerState::v_frame
    bne no_hi
    inc player + TPlayerState::v_frame+1
no_hi:
    rts
.endproc

.proc update_visuals
    lda player + TPlayerState::v_frame
    lsr
    lsr
    lsr
    lsr
    lsr
    and #$07
    sta BKGRND
    
    lda player + TPlayerState::v_frame+1
    asl
    asl
    asl
    and #$08
    ora BKGRND
    
    and #$0F
    asl
    asl
    asl
    asl
    ora #$08
    sta BKGRND
    rts
.endproc

; --- Vectors ---
.segment "VECTORS"
    .word reset
    .word reset
    .word reset
