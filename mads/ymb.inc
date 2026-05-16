; ============================================================
; ymb.inc -- INDUSTRIAL MUSIC ENGINE DEFINITIONS (MADS)
; ============================================================

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
