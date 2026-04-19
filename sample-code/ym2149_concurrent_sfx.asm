        processor 6502

; ============================================================
; YM2149 Concurrent SFX Player - "Jam" Edition
; ============================================================
; Background Music: AY-3-8910_Jam.vgm
; SFX: Laser (Procedural)
; ============================================================

ay_addr    = $4000
ay_data    = $4001
background = $0020
swcha      = $0280      ; Joystick Port 1

; -------------------------
; Zero Page "Track Control Blocks" (TCB)
; -------------------------

; --- Track A (Registers 0, 1, 8) ---
trA_ptr        = $80 ; word
trA_pat_frames = $82 ; byte
trA_seq_idx    = $83 ; byte
trA_seq_base   = $84 ; word
trA_pat_table  = $86 ; word
trA_pat_base   = $88 ; word
trA_pat_size   = $8A ; byte
trA_sfx_ptr    = $8B ; word
trA_sfx_active = $8D ; byte

; --- Track B (Registers 2, 3, 9) ---
trB_ptr        = $8E ; word
trB_pat_frames = $90 ; byte
trB_seq_idx    = $91 ; byte
trB_seq_base   = $92 ; word
trB_pat_table  = $94 ; word
trB_pat_base   = $96 ; word
trB_pat_size   = $98 ; byte

; --- Track C (Regs 4-7, 10-13) ---
trC_ptr        = $99 ; word
trC_pat_frames = $9B ; byte
trC_seq_idx    = $9C ; byte
trC_seq_base   = $9D ; word
trC_pat_table  = $9F ; word
trC_pat_base   = $A1 ; word
trC_pat_size   = $A3 ; byte

; Global
frame_cnt      = $A4 ; word
tmp_mask       = $A6
tmp_tcb        = $A7 ; word pointer
tmp_ptr        = $A9 ; word
tmp_val        = $AB ; word

        ifnconst build_with_header
build_with_header SET 1
        endif

        org $8000
        if build_with_header
                include "a78_ym2149_header.asm"
        endif

MusicData:
        ; Placeholder for music binary
        ; In a real build, this would be incbin'd
        ds.b 6, 0

reset:
        sei
        cld
        ldx #$ff
        txs

        ; Power-on Delay
        ldx #0
        ldy #0
p_wait: dex
        bne p_wait
        dey
        bne p_wait

        jsr init_ym
        ; jsr init_music ; Omitted for sample logic

main_loop:
        jsr sync_vbi
        
        ; --- SFX Trigger Check ---
        lda swcha
        and #$80        ; Fire button?
        bne .no_trigger
        jsr trigger_laser
.no_trigger:

        jsr play_frame
        jmp main_loop

; -------------------------
; Initializers
; -------------------------
init_ym:
        ldx #13
.loop:  stx ay_addr
        ldy #0
        cpx #7          ; Mixer?
        bne .not_mixer
        ldy #$ff        ; All off
.not_mixer:
        sty ay_data
        dex
        bpl .loop
        rts

trigger_laser:
        lda trA_sfx_active
        bne .done
        
        ; Point to Laser SFX
        lda #<Laser_SFX
        sta trA_sfx_ptr
        lda #>Laser_SFX
        sta trA_sfx_ptr+1
        
        lda #1
        sta trA_sfx_active
.done:  rts

; -------------------------
; Play Iteration
; -------------------------
play_frame:
        ; 1. Process Track A
        lda trA_sfx_active
        beq .do_music_A
        jsr update_sfx_track_A
        jmp .A_done
.do_music_A:
        ; (Music logic omitted for brevity, same as ym2149_player.asm)
.A_done:

        ; 2. Process Track B
        ; (Music logic omitted)

        ; 3. Process Track C
        ; (Music logic omitted)
        rts

; -------------------------
; SFX Engine (Surgical bit-loop)
; -------------------------
update_sfx_track_A:
        ldy #0
        lda (trA_sfx_ptr),y
        beq .end_sfx        ; $00 = end of SFX
        sta tmp_mask
        iny
        
        ; Bit 0 -> Reg 0
        lsr tmp_mask
        bcc .s0
        lda #0
        sta ay_addr
        lda (trA_sfx_ptr),y
        sta ay_data
        iny
.s0:    ; Bit 1 -> Reg 1
        lsr tmp_mask
        bcc .s1
        lda #1
        sta ay_addr
        lda (trA_sfx_ptr),y
        sta ay_data
        iny
.s1:    ; Bit 2 -> Reg 8
        lsr tmp_mask
        bcc .s8
        lda #8
        sta ay_addr
        lda (trA_sfx_ptr),y
        sta ay_data
        iny
.s8:    
        ; Advance SFX pointer
        tya
        clc
        adc trA_sfx_ptr
        sta trA_sfx_ptr
        lda #0
        adc trA_sfx_ptr+1
        sta trA_sfx_ptr+1
        rts
.end_sfx:
        lda #0
        sta trA_sfx_active
        rts

sync_vbi:
        ; Wait logic (50Hz or 60Hz)
        rts

; -------------------------
; Data Section
; -------------------------
Laser_SFX:
        ; Compiled from laser.json (Legacy 16-bit Mask Format)
        ; Frame 0: Mask $0007 (Regs 0, 1, 8), Data $32, $00, $0F
        dc.b $07, $00, $32, $00, $0F
        ; Frame 1: Mask $0001 (Reg 0), Data $56
        dc.b $01, $00, $56
        ; End Sentinel (2 null bytes)
        dc.b $00, $00

; -------------------------
; Vectors
; -------------------------
        org $fff8
        .byte $ff, $83
        org $fffa
        .word reset
        .word reset
        .word reset
