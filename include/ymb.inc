; ============================================================
; ymb.inc -- INDUSTRIAL MUSIC ENGINE DEFINITIONS (Universal)
; Compatible with DASM and MADS (Pro features)
; ============================================================

    .if MADS
; ; --- MADS Pro Features (Structures) ---
.struct TMusicHeader
    pat_size     .byte
    num_patterns .byte
    seq_len      .byte
.ends

.struct TPlayerState
    music_ptr   .word
    frame_cnt   .word
    pat_frames  .byte
    seq_idx     .byte
    tmp_mask    .word
    pat_table   .word
    pat_base    .word
    seq_base    .word
    pat_size    .byte
    music_acc   .word
    music_delta .word
    v_frame     .word
.ends
    .else
; --- DASM Baseline (Raw Offsets) ---
; Header for the Industrial Music Format (.ymb)
TMusicHeader_pat_size     = 0
TMusicHeader_num_patterns = 1
TMusicHeader_seq_len      = 2

; Internal State for the Fractional Playback Engine (Offsets)
TPlayerState_music_ptr   = 0
TPlayerState_frame_cnt   = 2
TPlayerState_pat_frames  = 4
TPlayerState_seq_idx     = 5
TPlayerState_tmp_mask    = 6
TPlayerState_pat_table   = 8
TPlayerState_pat_base    = 10
TPlayerState_seq_base    = 12
TPlayerState_pat_size    = 14
TPlayerState_music_acc   = 15
TPlayerState_music_delta = 17
TPlayerState_v_frame     = 19
TPlayerState_SIZE        = 21
    .endif
