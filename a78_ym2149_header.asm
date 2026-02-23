; ============================================================
; Atari 7800 A78 Header (v4)
; YM2149 PSG enabled (address decode depends on build/mapper)
; ============================================================

.ROMSIZE = $8000          ; 32K ROM

        if ( . & $FF ) = 0
            ORG (. - 128),0
        else
            ORG .,0
        endif

        SEG ROM
.HEADER = .

; -------------------------
; Header version + magic
; -------------------------
        DC.B    4                  ; header version
        DC.B    "ATARI7800"         ; magic string (padded automatically)

; -------------------------
; Game title (32 bytes)
; -------------------------
        ORG .HEADER+$11,0
        DC.B    "YM2149 TEST ROM"
        DS.B    32-15,0             ; zero-pad

; -------------------------
; ROM size (bytes 49–52)
; -------------------------
        ORG .HEADER+$31,0
        DC.B    (.ROMSIZE>>24)
        DC.B    (.ROMSIZE>>16)&$FF
        DC.B    (.ROMSIZE>>8)&$FF
        DC.B    (.ROMSIZE)&$FF

; -------------------------
; Legacy v3 cart type bytes
; (kept zeroed)
; -------------------------
        DC.B    %00000000           ; type A
        DC.B    %00000000           ; type B

; -------------------------
; Controllers
; -------------------------
        DC.B    1                  ; port 1 = 7800 joystick
        DC.B    1                  ; port 2 = 7800 joystick

; -------------------------
; TV format
; -------------------------
        DC.B    %00000000           ; NTSC, single-region

; -------------------------
; Save peripherals
; -------------------------
        DC.B    %00000000           ; none

; -------------------------
; IRQ sources (deprecated)
; -------------------------
        ORG .HEADER+62,0
        DC.B    %00000000

; -------------------------
; Slot passthrough
; -------------------------
        DC.B    %00000000           ; no XM

; -------------------------
; v4 mapper info
; -------------------------
        DC.B    0                  ; mapper = linear
        DC.B    0                  ; mapper options

; -------------------------
; Audio flags (v4)
; -------------------------
        DC.B    %01000000           ; audio_hi: bit 6 = YM2149 present
        DC.B    %00000000           ; audio_lo

; -------------------------
; Interrupt flags (v4)
; -------------------------
        DC.B    %00000000
        DC.B    %00000000

; -------------------------
; Footer
; -------------------------
        ORG .HEADER+100,0
        DC.B    "ACTUAL CART DATA STARTS HERE"

; -------------------------
; Restore code origin
; -------------------------
        ORG $8000,0
