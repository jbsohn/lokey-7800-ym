        processor 6502

; ---------------------------------------------------------
; YM2149 MVP Player for Atari 7800
; ---------------------------------------------------------
; Simple, high-precision delay-based player for the 60Hz MVP.
; ---------------------------------------------------------

ay_addr    = $4000
ay_data    = $4001
background = $0020 
mstat      = $0028

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
music_acc  = $8f ; 2 bytes
music_delta = $91 ; 2 bytes
v_frame    = $93 ; 2 bytes

; Constants
NUM_REGS   = 14

        ; These are injected via -D from the Makefile
        include MUSIC_INC

        ifnconst build_with_header
build_with_header SET 1
        endif

        org $8000

        if build_with_header
            include "a78_ym2149_header.asm"
        endif

; ---------------------------------------------------------
; Music Data
; ---------------------------------------------------------
MusicData:
        incbin MUSIC_BIN

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
        jsr update_visuals
        
        ; 16-bit Fractional Music Step (Converts 60Hz frame to PLAYER_HZ)
        clc
        lda music_acc
        adc music_delta
        sta music_acc
        lda music_acc+1
        adc music_delta+1
        sta music_acc+1
        bcc .skip
        jsr play_frame
.skip:
        jmp main_loop

; ---------------------------------------------------------
; Utilities
; ---------------------------------------------------------

sync_vbi:
        ; Wait for EXISTING VBlank to end
.v1:    bit mstat
        bmi .v1
        ; Wait for NEW VBlank to start
.v2:    bit mstat
        bpl .v2
        
        ; Increment 60Hz counter for visuals
        inc v_frame
        bne .no_hi
        inc v_frame+1
.no_hi:
        rts

update_visuals:
        ; 1. Stable 0.5-second Color Cycle (32 frames)
        ; Bit 5 of v_frame changes every 32 frames (0.53s)
        lda v_frame
        lsr
        lsr
        lsr
        lsr
        lsr          ; Bits 5, 6, 7 are now at positions 0, 1, 2
        and #$07     ; Keep only those 3 bits
        sta background
        lda v_frame+1
        asl
        asl
        asl
        and #$08     ; Bit 0 of high byte (bit 8) moves to position 3
        ora background
        
        ; Scale the 4-bit result into the high nibble for the Hue
        and #$0F
        asl
        asl
        asl
        asl
        ora #$08 ; Constant Medium Brightness
        
        ; 2. Already synced to VBlank by sync_vbi
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
        sta music_acc
        sta music_acc+1
        sta v_frame
        sta v_frame+1

        ; Pre-calculate 16-bit music step: (PLAYER_HZ * 65536) / 60
        ; We store the fractional 16-bit delta
music_step = ( (PLAYER_HZ * 65536) / 60 )
        lda #<music_step
        sta music_delta
        lda #>music_step
        sta music_delta+1

        ; Header: [pat_size][num_patterns][seq_len]
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
        
        ; Fetch next pattern from sequence (Strict 8-bit)
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
