        processor 6502

; ---------------------------------------------------------
; YM2149 MVP Player for Atari 7800
; ---------------------------------------------------------
; Simple, high-precision delay-based player for the 60Hz MVP.
; Supports optional SFX interrupt.
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

; SFX State
sfx_ptr    = $90 ; word
sfx_active = $92 ; byte (0=Music, 1=SFX)

tmp_ptr    = $94 ; word

; Constants
NUM_REGS   = 14

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
        
        ; 1. Always process music (Music advances even during SFX)
        jsr play_music_frame
        
        ; 2. If SFX is active, process it (Overwrites music writes)
        lda sfx_active
        beq .no_sfx
        
        lda #<sfx_ptr
        sta tmp_ptr
        jsr play_frame_core
        bcs .no_sfx        ; Carry Set = SFX still playing
        
        lda #0             ; Carry Clear = SFX ended
        sta sfx_active
        
.no_sfx:
        jsr update_visuals
        jmp main_loop

; ---------------------------------------------------------
; Utilities
; ---------------------------------------------------------

sync_vbi:
        ldy #YM_DELAY
.d1:    ldx #$00
.d2:    dex
        bne .d2
        dey
        bne .d1
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
        sta sfx_active

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
; Play a single music frame
; ---------------------------------------------------------
play_music_frame:
        ; Check End
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

.do_play:
        dec pat_frames
        lda #<music_ptr
        sta tmp_ptr
        jsr play_frame_core
        inc frame_cnt
        bne .p_done
        inc frame_cnt+1
.p_done:
        rts

; Core routine that processes a bitmask packet at (tmp_ptr)
; tmp_ptr is a pointer to the ZP pointer (music_ptr or sfx_ptr)
; Returns Carry Clear if stream ended (for SFX)
tmp_ptr_ptr = $A4 ; Helper
play_frame_core:
        ldx tmp_ptr
        stx tmp_ptr_ptr
        lda #0
        sta tmp_ptr_ptr+1
        
        ldy #0
        lda (tmp_ptr_ptr),y ; Low ptr
        sta tmp_ptr
        iny
        lda (tmp_ptr_ptr),y ; High ptr
        sta tmp_ptr+1
        
        ldy #0
        lda (tmp_ptr),y
        sta tmp_mask
        iny
        lda (tmp_ptr),y
        sta tmp_mask+1
        
        ; If both masks are 0, end of stream
        ora tmp_mask
        beq .stream_end
        
        clc
        lda tmp_ptr
        adc #2
        sta tmp_ptr
        lda tmp_ptr+1
        adc #0
        sta tmp_ptr+1

        ldx #0
.reg_loop:
        cpx #8
        bcc .low_byte
        txa
        sec
        sbc #8
        tay
        lda bit_table,y
        and tmp_mask+1
        beq .next_reg
        jmp .update_reg
.low_byte:
        txa
        tay
        lda bit_table,y
        and tmp_mask
        beq .next_reg
.update_reg:
        stx ay_addr
        ldy #0
        lda (tmp_ptr),y
        sta ay_data
        inc tmp_ptr
        bne .next_reg
        inc tmp_ptr+1
.next_reg:
        inx
        cpx #NUM_REGS
        bne .reg_loop
        
        ; Save back the updated pointer
        ldy #0
        lda tmp_ptr
        sta (tmp_ptr_ptr),y
        iny
        lda tmp_ptr+1
        sta (tmp_ptr_ptr),y
        sec ; Still active
        rts

.stream_end:
        clc ; Ended
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
