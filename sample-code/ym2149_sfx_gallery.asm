        processor 6502

; ---------------------------------------------------------
; YM2149 SFX Gallery for Atari 7800
; ---------------------------------------------------------
; Plays all compiled sounds in a loop with a delay.
; ---------------------------------------------------------

ay_addr    = $4000
ay_data    = $4001
background = $0020 
mstat      = $0028

; Zero Page
sfx_ptr    = $80 ; word
sfx_active = $82 ; byte
sfx_idx    = $83 ; byte
delay_cnt  = $84 ; byte
tmp_mask   = $86 ; 2 bytes
tmp_ptr    = $88 ; word
v_frame    = $8A ; 2 bytes

; Constants
NUM_REGS   = 14
DELAY_TIME = 60    ; 1 second at 60Hz

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

        ; Power-on Delay
        ldx #$00
        ldy #$00
p_1:    dex
        bne p_1
        dey
        bne p_1

        ; 1. Hardware Initialization
        lda #0
        sta sfx_active
        sta sfx_idx
        sta v_frame
        sta v_frame+1
        lda #DELAY_TIME
        sta delay_cnt
        
        jsr silence_ym

main_loop:
        jsr sync_vbi
        jsr update_visuals

        ; 1. Check if we need to trigger next sound
        lda sfx_active
        bne .do_sfx
        
        lda delay_cnt
        beq .trigger
        dec delay_cnt
        jmp .done

.trigger:
        jsr trigger_next
        lda #DELAY_TIME
        sta delay_cnt
        jmp .done

.do_sfx:
        lda #<sfx_ptr
        sta tmp_ptr
        jsr play_frame_core
        bcs .done
        lda #0
        sta sfx_active
        jsr silence_ym    ; Clean up after sound ends

.done:
        jmp main_loop

; ---------------------------------------------------------
; Logic
; ---------------------------------------------------------

silence_ym:
        ldx #NUM_REGS-1
.loop:  stx ay_addr
        ldy #0
        cpx #7          ; Mixer?
        bne .vols
        ldy #$ff        ; All off
.vols:  cpx #8          ; Volumes 8, 9, 10
        bcc .set
        cpx #11
        bcs .set
        ldy #0          ; Volume 0
.set:   sty ay_data
        dex
        bpl .loop
        rts

trigger_next:
        ldy sfx_idx
        
        ; Get pointer from table
        tya
        asl
        tay
        lda sfx_table,y
        sta sfx_ptr
        lda sfx_table+1,y
        sta sfx_ptr+1
        
        lda #1
        sta sfx_active
        
        inc sfx_idx
        lda sfx_idx
        cmp #NUM_SOUNDS
        bcc .rts
        lda #0
        sta sfx_idx
.rts:   rts

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
        inc v_frame
        bne .no_hi
        inc v_frame+1
.no_hi:
        rts

update_visuals:
        ; Take bits 2,3,4,5 and move them to 4,5,6,7
        lda v_frame
        asl
        asl
        and #$F0
        ora #$08 ; Medium brightness
        sta background
        rts

; Core routine that processes a bitmask packet at (tmp_ptr)
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
; Data Section
; ---------------------------------------------------------

sfx_table:
        .word Coin_Start
        .word Explosion_Start
        .word Fail_Start
        .word PowerUp_Start
        .word Laser_Start
NUM_SOUNDS = 5

        include "all_sounds.asm"

; ---------------------------------------------------------
; 7800 ROM Footer
; ---------------------------------------------------------
        org $fff8
        .byte $ff, $83
        org $fffa
        .word reset ; NMI
        .word reset ; RESET
        .word reset ; IRQ
