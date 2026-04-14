        processor 6502

; ---------------------------------------------------------
; YM2149 MVP Player for Atari 7800
; ---------------------------------------------------------
; Simple, high-precision delay-based player for the 60Hz MVP.
; ---------------------------------------------------------

ay_addr    = $4000
ay_data    = $4001
background = $0020 

; Zero Page
music_ptr  = $80 
frame_cnt  = $82 
pat_frames = $84 
seq_idx    = $85 
tmp_mask   = $86 
pat_table  = $88 
pat_base   = $8a 
seq_base   = $8c 
pat_size   = $8e 

; Constants
NUM_REGS   = 14
        include "enchant1.yminc"

        ifnconst build_with_header
build_with_header SET 1
        endif

        org $8000

        if build_with_header
            include "a78_ym2149_header.asm"
        endif

; ---------------------------------------------------------
; Music Data (At start of ROM after header)
; ---------------------------------------------------------
MusicData:
        incbin "enchant1.bin"

; ---------------------------------------------------------
; Entry Point
; ---------------------------------------------------------
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

        ; 1. Hardware Initialization
        jsr init_music
        
        ; Clear YM Registers
        ldx #NUM_REGS-1
.clear:
        stx ay_addr
        lda #0
        sta ay_data
        dex
        bpl .clear

main_loop:
        jsr sync_vbi
        jsr play_frame
        jsr update_visuals
        jmp main_loop

; ---------------------------------------------------------
; Utilities
; ---------------------------------------------------------

sync_vbi:
        ; 1. Coarse Delay (Y steps)
        ldy #YM_DELAY
.d1:    ldx #$00
.d2:    dex
        bne .d2
        dey
        bne .d1

        ; 2. Fine Delay (X steps)
        ldx #YM_FINE
        beq .done
.d3:    dex
        bne .d3
.done:
        rts

update_visuals:
        lda frame_cnt
        and #$0F
        ora #$40 
        sta background
        rts

; ---------------------------------------------------------
; Initialize Music Pointers from Header
; ---------------------------------------------------------
init_music:
        lda #0
        sta frame_cnt
        sta frame_cnt+1
        sta seq_idx
        sta pat_frames ; Force new pattern fetch

        ; pat_size is stored as first byte of MusicData
        lda MusicData
        sta pat_size

        ; seq_base = MusicData + 3
        clc
        lda #<MusicData
        adc #3
        sta seq_base
        lda #>MusicData
        adc #0
        sta seq_base+1

        ; pat_table = seq_base + seq_len
        clc
        lda seq_base
        adc MusicData+2 ; seq_len
        sta pat_table
        lda seq_base+1
        adc #0
        sta pat_table+1

        ; pat_base = pat_table + num_patterns * 2
        lda MusicData+1 ; num_patterns
        asl ; * 2
        tay ; Save low byte of (num_patterns * 2) in Y
        lda #0
        rol ; Get carry from asl
        tax ; Save high byte of (num_patterns * 2) in X
        
        clc
        tya
        adc pat_table
        sta pat_base
        txa
        adc pat_table+1
        sta pat_base+1
        rts

; ---------------------------------------------------------
; Play a single compressed frame
; ---------------------------------------------------------
play_frame:
        ; Check if we reached the end of the song
        lda frame_cnt+1
        cmp #>MAX_FRAMES
        bne .check_pattern
        lda frame_cnt
        cmp #<MAX_FRAMES
        bcc .check_pattern
        
        jsr init_music

.check_pattern:
        lda pat_frames
        bne .do_play
        
        ; Fetch next pattern from sequence
        ldy seq_idx
        lda (seq_base),y
        inc seq_idx
        
        ; Calculate offset in pat_table
        asl ; index * 2
        tay
        lda (pat_table),y
        sta music_ptr
        iny
        lda (pat_table),y
        sta music_ptr+1
        
        ; Add pat_base to music_ptr
        clc
        lda music_ptr
        adc pat_base
        sta music_ptr
        lda music_ptr+1
        adc pat_base+1
        sta music_ptr+1
        
        lda pat_size
        sta pat_frames

.do_play:
        dec pat_frames

        ; 1. Read Mask (2 bytes)
        ldy #0
        lda (music_ptr),y
        sta tmp_mask
        iny
        lda (music_ptr),y
        sta tmp_mask+1
        
        ; Advance pointer past mask
        clc
        lda music_ptr
        adc #2
        sta music_ptr
        lda music_ptr+1
        adc #0
        sta music_ptr+1

        ldx #0 ; Current Register Index
.reg_loop:
        ; Check bit in mask
        cpx #8
        bcc .low_byte
        
        ; High Byte (Reg 8-13)
        txa
        sec
        sbc #8
        tay ; Y = bit index 0-5
        lda bit_table,y
        and tmp_mask+1
        beq .next_reg
        jmp .update_reg

.low_byte:
        txa
        tay ; Y = bit index 0-7
        lda bit_table,y
        and tmp_mask
        beq .next_reg

.update_reg:
        stx ay_addr
        ldy #0
        lda (music_ptr),y
        sta ay_data
        
        inc music_ptr
        bne .next_reg
        inc music_ptr+1

.next_reg:
        inx
        cpx #NUM_REGS
        bne .reg_loop

        inc frame_cnt
        bne .p_done
        inc frame_cnt+1
.p_done:
        rts

bit_table:
        .byte $01, $02, $04, $08, $10, $20, $40, $80

; ---------------------------------------------------------
; 7800 ROM Footer
; ---------------------------------------------------------
        org $fff8
        .byte $ff, $83
        org $fffa
        .word reset ; NMI
        .word reset ; RESET
        .word reset ; IRQ
