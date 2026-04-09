        processor 6502

; ---------------------------------------------------------
; YM2149 Stream Player for Atari 7800
; ---------------------------------------------------------
; Streams compressed bitmask frames from a pre-processed YM2 binary.
; Target: 32KB ROM ($8000-$FFFF)
; ---------------------------------------------------------

ay_addr    = $4000
ay_data    = $4001
background = $0020

; Zero Page
music_ptr  = $80 ; 16-bit pointer to current stream position
frame_cnt  = $82 ; 16-bit frame counter
vbi_div    = $84
tmp_mask   = $86 ; 16-bit mask temporary storage

; Constants
NUM_REGS   = 14
        include "ancool1_test.inc"

        ifnconst build_with_header
build_with_header SET 1
        endif

        org $8000

        if build_with_header
            include "a78_ym2149_header.asm"
        endif

reset:
        sei
        cld
        ldx #$ff
        txs

        ; Power-on Delay (For hardware stability)
        ldx #$00
        ldy #$00
p_1:    dex
        bne p_1
        dey
        bne p_1

        ; 1. Hardware Initialization
        lda #<MusicData
        sta music_ptr
        lda #>MusicData
        sta music_ptr+1
        
        lda #0
        sta frame_cnt
        sta frame_cnt+1

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
; Play a single compressed frame to the YM2149
; ---------------------------------------------------------
play_frame:
        ; Check if we reached the end
        lda frame_cnt+1
        cmp #>MAX_FRAMES
        bne .do_play
        lda frame_cnt
        cmp #<MAX_FRAMES
        bcc .do_play
        
        ; Loop back to start
        lda #<MusicData
        sta music_ptr
        lda #>MusicData
        sta music_ptr+1
        lda #0
        sta frame_cnt
        sta frame_cnt+1

.do_play:
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
        bne .done
        inc frame_cnt+1
.done:
        rts

bit_table:
        .byte $01, $02, $04, $08, $10, $20, $40, $80

; ---------------------------------------------------------
; Utilities
; ---------------------------------------------------------
sync_vbi:
        ; Simple delay to approximate Hz
        ldy #YM_DELAY
.d1:    ldx #$00
.d2:    dex
        bne .d2
        dey
        bne .d1
        rts

update_visuals:
        lda frame_cnt
        and #$0F
        ora #$40 
        sta background
        rts

; ---------------------------------------------------------
; Music Data (Compressed)
; ---------------------------------------------------------
        org $8800 
MusicData:
        incbin "ancool1_test.bin"

; ---------------------------------------------------------
; 7800 ROM Footer
; ---------------------------------------------------------
        org $fff8
        .byte $ff, $83
        org $fffa
        .word reset ; NMI
        .word reset ; RESET
        .word reset ; IRQ
