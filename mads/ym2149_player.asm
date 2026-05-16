; =========================================================
; YM2149 MVP Player for Atari 7800 (MADS port)
; =========================================================
    opt h- f+
    icl "maria.inc"
    icl "ym2149.inc"
    icl "ymb.inc"

    org $40
player TPlayerState

    org $8000

reset
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
p1  dex
    bne p1
    dey
    bne p1

    ; Clear YM Registers
    ldx #NUM_REGS-1
cl_y
    stx AY_ADDR
    lda #0
    sta AY_DATA
    dex
    bpl cl_y

    jsr init_music

main_loop
    jsr sync_vbi
    jsr update_visuals
    
    ; 16-bit Fractional Music Step
    clc
    lda player.music_acc
    adc player.music_delta
    sta player.music_acc
    lda player.music_acc+1
    adc player.music_delta+1
    sta player.music_acc+1
    bcc skip
    jsr play_frame
skip
    jmp main_loop

; --- Music Core ---

init_music
    lda #0
    ldx #.sizeof(TPlayerState)-1
cl_p 
    sta player,x
    dex
    bpl cl_p

    ; Use pre-calculated music delta
    lda #<STEP
    sta player.music_delta
    lda #>STEP
    sta player.music_delta+1

    lda MusicData
    sta player.pat_size

    clc
    lda #<MusicData
    adc #3
    sta player.seq_base
    lda #>MusicData
    adc #0
    sta player.seq_base+1

    ldy #2 ; seq_len offset
    clc
    lda player.seq_base
    adc MusicData,y
    sta player.pat_table
    lda #0
    adc player.seq_base+1
    sta player.pat_table+1

    ldy #1 ; num_patterns offset
    lda MusicData,y
    asl
    tay
    lda #0
    rol
    tax
    
    clc
    tya
    adc player.pat_table
    sta player.pat_base
    txa
    adc player.pat_table+1
    sta player.pat_base+1
    rts

play_frame
    lda player.frame_cnt+1
    cmp #>MAX_FRAMES
    bne check_pattern
    lda player.frame_cnt
    cmp #<MAX_FRAMES
    bcc check_pattern
    
    jsr init_music

check_pattern
    lda player.pat_frames
    bne do_play
    ldy player.seq_idx
    lda (player.seq_base),y
    inc player.seq_idx
    asl
    tay
    lda (player.pat_table),y
    sta player.music_ptr
    iny
    lda (player.pat_table),y
    sta player.music_ptr+1
    clc
    lda player.music_ptr
    adc player.pat_base
    sta player.music_ptr
    lda player.music_ptr+1
    adc player.pat_base+1
    sta player.music_ptr+1
    lda player.pat_size
    sta player.pat_frames
do_play
    dec player.pat_frames
    ldy #0
    lda (player.music_ptr),y
    sta player.tmp_mask
    iny
    lda (player.music_ptr),y
    sta player.tmp_mask+1
    clc
    lda player.music_ptr
    adc #2
    sta player.music_ptr
    lda player.music_ptr+1
    adc #0
    sta player.music_ptr+1
    ldx #0
reg_loop
    cpx #8
    bcc low
    txa
    sec
    sbc #8
    tay
    lda bit_table,y
    and player.tmp_mask+1
    beq next
    bne upd
low
    txa
    tay
    lda bit_table,y
    and player.tmp_mask
    beq next
upd
    stx AY_ADDR
    ldy #0
    lda (player.music_ptr),y
    sta AY_DATA
    inc player.music_ptr
    bne next
    inc player.music_ptr+1
next
    inx
    cpx #NUM_REGS
    bne reg_loop
    inc player.frame_cnt
    bne p_done
    inc player.frame_cnt+1
p_done
    rts

bit_table dta b($01,$02,$04,$08,$10,$20,$40,$80)

sync_vbi
v1  bit MSTAT
    bmi v1
v2  bit MSTAT
    bpl v2

    inc player.v_frame
    bne no_hi
    inc player.v_frame+1
no_hi
    rts

update_visuals
    lda player.v_frame
    lsr
    lsr
    lsr
    lsr
    lsr
    and #$07
    sta BKGRND
    
    lda player.v_frame+1
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

; --- Vectors (Only if not being included as part of a larger file that adds them later) ---
; But here we want the song.asm to have them.
; I'll put them in a separate block or just at the end.
.if .not .def PLAYER_INC
    .align $fff8, $ff
    dta b($ff, $83)
    dta a(reset, reset, reset)
.endif
