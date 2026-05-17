; =========================================================
; player.asm -- YM2149 MVP Player for Atari 7800 (Universal)
; =========================================================

    .if MADS
      opt h- f+
      icl "maria.inc"
      icl "ym2149.inc"
      icl "ymb.inc"
      blk none $8000
    .else
      processor 6502
      include "maria.inc"
      include "ym2149.inc"
      include "ymb.inc"
      org $8000
    .endif

music_ptr   = $80 
frame_cnt   = $82 
pat_frames  = $84 
seq_idx     = $85 
tmp_mask    = $86 
pat_table   = $88 
pat_base    = $8a 
seq_base    = $8c 
pat_size    = $8e 
music_acc   = $8f ; 2 bytes
music_delta = $91 ; 2 bytes
v_frame     = $93 ; 2 bytes

    .if MADS
        icl "music_bin.inc"
    .else
        include "music_bin.inc"
    .endif

reset:
        sei
        cld
        ldx #$ff
        txs

        ldx #$00
        ldy #$00
p_1:    dex
        bne p_1
        dey
        bne p_1

        jsr init_music
        
        ldx #NUM_REGS-1
cl_y:
        stx AY_ADDR
        lda #0
        sta AY_DATA
        dex
        bpl cl_y

main_loop:
        jsr sync_vbi
        jsr update_visuals
        
        clc
        lda music_acc
        adc music_delta
        sta music_acc
        lda music_acc+1
        adc music_delta+1
        sta music_acc+1
        bcc _skip
        jsr play_frame
_skip:
        jmp main_loop

sync_vbi:
    .if MADS
v1:     bit MSTAT
        bmi v1
v2:     bit MSTAT
        bpl v2
    .else
v1:     bit $0028
        bmi v1
v2:     bit $0028
        bpl v2
    .endif
        
        inc v_frame
        bne _no_hi
        inc v_frame+1
_no_hi:
        rts

update_visuals:
        lda v_frame
        lsr
        lsr
        lsr
        lsr
        lsr
        and #$07
        sta BKGRND
        lda v_frame+1
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

init_music:
        lda #0
        sta frame_cnt
        sta frame_cnt+1
        sta seq_idx
        sta pat_frames 
        sta music_acc
        sta music_acc+1
        sta v_frame
        sta v_frame+1

music_step = ( (PLAYER_HZ * 65536) / 60 )

        lda #<music_step
        sta music_delta
        lda #>music_step
        sta music_delta+1

        lda MusicData
        sta pat_size

        clc
        lda #<MusicData
        adc #3
        sta seq_base
        lda #>MusicData
        adc #0
        sta seq_base+1

        clc
        lda seq_base
        adc MusicData+2
        sta pat_table
        lda seq_base+1
        adc #0
        sta pat_table+1

        lda MusicData+1
        asl
        tay
        lda #0
        rol
        tax
        
        clc
        tya
        adc pat_table
        sta pat_base
        txa
        adc pat_table+1
        sta pat_base+1
        rts

play_frame:
        lda frame_cnt+1
        cmp #>MAX_FRAMES
        bne _check_pattern
        lda frame_cnt
        cmp #<MAX_FRAMES
        bcc _check_pattern
        
        jsr init_music

_check_pattern:
        lda pat_frames
        bne _do_play
        
        ldy seq_idx
        lda (seq_base),y
        inc seq_idx
        
        asl
        tay
        lda (pat_table),y
        sta music_ptr
        iny
        lda (pat_table),y
        sta music_ptr+1
        
        clc
        lda music_ptr
        adc pat_base
        sta music_ptr
        lda music_ptr+1
        adc pat_base+1
        sta music_ptr+1
        
        lda pat_size
        sta pat_frames

_do_play:
        dec pat_frames

        ldy #0
        lda (music_ptr),y
        sta tmp_mask
        iny
        lda (music_ptr),y
        sta tmp_mask+1
        
        clc
        lda music_ptr
        adc #2
        sta music_ptr
        lda music_ptr+1
        adc #0
        sta music_ptr+1

        ldx #0
_reg_loop:
        cpx #8
        bcc _low_byte
        
        txa
        sec
        sbc #8
        tay
        lda bit_table,y
        and tmp_mask+1
        beq _next_reg
        jmp _update_reg

_low_byte:
        txa
        tay
        lda bit_table,y
        and tmp_mask
        beq _next_reg

_update_reg:
        stx AY_ADDR
        ldy #0
        lda (music_ptr),y
        sta AY_DATA
        
        inc music_ptr
        bne _next_reg
        inc music_ptr+1

_next_reg:
        inx
        cpx #NUM_REGS
        bne _reg_loop

        inc frame_cnt
        bne _p_done
        inc frame_cnt+1
_p_done:
        rts

bit_table:
        .byte $01, $02, $04, $08, $10, $20, $40, $80

    .if MADS
        .align $fff8, $ff
        .byte $ff, $83
        .word reset, reset, reset
    .else
        org $fff8, $ff
        .byte $ff, $83
        .word reset
        .word reset
        .word reset
    .endif
