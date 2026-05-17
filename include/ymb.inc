; ============================================================
; ymb.inc -- YM2149 Music
; Compatible with DASM and MADS
; ============================================================

; --- Header for the Industrial Music Format (.ymb) ---
TMUSIC_HEADER_PAT_SIZE     = 0
TMUSIC_HEADER_NUM_PATTERNS = 1
TMUSIC_HEADER_SEQ_LEN      = 2
TMUSIC_HEADER_SIZE         = 3

; --- Internal State Offsets (Universal) ---
TPLAYER_STATE_MUSIC_PTR   = 0
TPLAYER_STATE_FRAME_CNT   = 2
TPLAYER_STATE_PAT_FRAMES  = 4
TPLAYER_STATE_SEQ_IDX     = 5
TPLAYER_STATE_TMP_MASK    = 6
TPLAYER_STATE_PAT_TABLE   = 8
TPLAYER_STATE_PAT_BASE    = 10
TPLAYER_STATE_SEQ_BASE    = 12
TPLAYER_STATE_PAT_SIZE    = 14
TPLAYER_STATE_MUSIC_ACC   = 15
TPLAYER_STATE_MUSIC_DELTA = 17
TPLAYER_STATE_V_FRAME     = 19
TPLAYER_STATE_SIZE        = 21

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
    .endif
