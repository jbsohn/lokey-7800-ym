; ============================================================
; ymb.inc -- INDUSTRIAL MUSIC ENGINE DEFINITIONS (ca65)
; CUSTOM SOFTWARE EXTENSION FOR ATARI 7800
; ============================================================

; Header for the Industrial Music Format (.ymb)
.struct TMusicHeader
    pat_size     .byte  ; Frames per pattern
    num_patterns .byte  ; Total number of patterns
    seq_len      .byte  ; Length of the sequence table
.endstruct

; Internal State for the Fractional Playback Engine
.struct TPlayerState
    music_ptr   .word   ; Current pointer into active pattern
    frame_cnt   .word   ; 16-bit master frame counter
    pat_frames  .byte   ; Frames remaining in current pattern
    seq_idx     .byte   ; Index into sequence table
    tmp_mask    .word   ; Buffer for YM register update mask
    pat_table   .word   ; Base of pattern pointer table
    pat_base    .word   ; Base of pattern data
    seq_base    .word   ; Base of sequence table
    pat_size    .byte   ; Static pattern size (from header)
    music_acc   .word   ; Fractional accumulator (16-bit)
    music_delta .word   ; Fractional step per frame (Rate * 65536 / 60)
    v_frame     .word   ; Generic visual frame counter
.endstruct
