        processor 6502

; -------------------------
; YM2149 write-only ports
; -------------------------
ay_addr = $4000
ay_data = $4001
background = $0020
maria_mstat = $0028
heartbeat = $80
note_index = $81
frame_div = $82
vblank_state = $83
vbi_work_div = $84

; Update melody every N VBLANKs (~60 Hz)
; Run VBI work once every N VBLANKs (master speed control)
vbi_work_every = 4
speed_frames = 12
heartbeat_colors = 16

; -------------------------
; Notes (YM2149 tone period = f_clk / (16 * f_out))
; f_clk ≈ 1.789772 MHz
; C4 ~ 261.6 Hz -> period ≈ 427
; E4 ~ 329.6 Hz -> period ≈ 339
; G4 ~ 392.0 Hz -> period ≈ 285
; -------------------------
note_c_lo = <427
note_c_hi = >427
note_e_lo = <339
note_e_hi = >339
note_g_lo = <285
note_g_hi = >285

        org $8000

        ; -------------------------
        ; A78 HEADER (v4)
        ; -------------------------
        ifnconst build_with_header
build_with_header SET 1
        endif

        if build_with_header
                include "a78_ym2149_header.asm"
        endif

reset:
        sei
        cld
        ldx #$ff
        txs

        lda #$00
        sta heartbeat
        sta note_index
        sta frame_div
        sta vbi_work_div
        lda maria_mstat
        and #$80
        sta vblank_state
        jsr update_heartbeat

        ; -------------------------
        ; YM2149 init
        ; -------------------------
        ; Disable noise, enable tone A only
        lda #$07
        sta ay_addr
        lda #%00111110
        sta ay_data

        ; Set volume A to max (fixed volume)
        lda #$08
        sta ay_addr
        lda #$0f
        sta ay_data

        ; Seed first note so audio starts immediately
        ldx #$00
        jsr load_note_a

main_loop:
        jsr wait_vblank
        jsr vbi_tick
        jmp main_loop

; -------------------------
; NMI / IRQ handlers (unused in this polled-VBI sample)
; -------------------------
nmi:
        rti

irq:
        rti

; -------------------------
; Once-per-VBLANK work
; -------------------------
vbi_tick:
        pha
        txa
        pha
        tya
        pha

        inc vbi_work_div
        lda vbi_work_div
        cmp #vbi_work_every
        bcc vbi_done
        lda #$00
        sta vbi_work_div

        jsr update_heartbeat

        inc frame_div
        lda frame_div
        cmp #speed_frames
        bcc vbi_done

        lda #$00
        sta frame_div

        ldx note_index
        inx
        cpx #$03
        bcc note_ok
        ldx #$00
note_ok:
        stx note_index
        jsr load_note_a

vbi_done:
        pla
        tay
        pla
        tax
        pla
        rts

; -------------------------
; Wait for next VBLANK phase (MSTAT bit 7 = 1)
; Includes timeout fallback if MSTAT is not changing.
; -------------------------
wait_vblank:
        ldy #$ff
wait_vblank_loop:
        lda maria_mstat
        and #$80
        cmp vblank_state
        bne vblank_toggled
        dey
        bne wait_vblank_loop
        jsr delay_fallback
        rts

vblank_toggled:
        sta vblank_state
        beq wait_vblank
        rts

; -------------------------
; Write tone A from note table index in X
; -------------------------
load_note_a:
        lda #$00
        sta ay_addr
        lda note_table_lo,x
        sta ay_data

        lda #$01
        sta ay_addr
        lda note_table_hi,x
        sta ay_data
        rts

; -------------------------
; Advance a simple color heartbeat each VBI
; -------------------------
update_heartbeat:
        ldx heartbeat
        lda heartbeat_table,x
        sta background
        inx
        cpx #heartbeat_colors
        bcc heartbeat_ok
        ldx #$00
heartbeat_ok:
        stx heartbeat
        rts

delay_fallback:
        ldx #$ff
delay_fallback_x:
        dex
        bne delay_fallback_x
        rts

note_table_lo:
        .byte note_c_lo, note_e_lo, note_g_lo
note_table_hi:
        .byte note_c_hi, note_e_hi, note_g_hi
; Double-pulse heartbeat: quick bright pulse, dip, second pulse, then recovery/pause
heartbeat_table:
        .byte $08, $1a, $3c, $74, $38, $12, $52, $76
        .byte $28, $0c, $06, $06, $06, $06, $06, $06

        org $fff8
        .byte $ff         ; required by 7800sign
        .byte $83         ; ROM start $8000
        org $fffa
        .word nmi
        .word reset
        .word irq
