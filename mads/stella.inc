; *************************************************
; *                                               *
; *   stella.inc -- 2600 GAME SYSTEM EQUATES      *
; *                                               *
; *************************************************

; --- STELLA (TIA) REGISTER ADDRESSES ---

VSYNC       = $00
VBLANK      = $01
WSYNC       = $02
RSYNC       = $03
NUSIZ0      = $04
NUSIZ1      = $05
COLUP0      = $06
COLUP1      = $07
COLUPF      = $08
COLUBK      = $09
CTRLPF      = $0A
REFP0       = $0B
REFP1       = $0C
PF0         = $0D
PF1         = $0E
PF2         = $0F
RESP0       = $10
RESP1       = $11
RESM0       = $12
RESM1       = $13
RESBL       = $14
AUDC0       = $15
AUDC1       = $16
AUDF0       = $17
AUDF1       = $18
AUDV0       = $19
AUDV1       = $1A
GRP0        = $1B
GRP1        = $1C
ENAM0       = $1D
ENAM1       = $1E
ENABL       = $1F
HMP0        = $20
HMP1        = $21
HMM0        = $22
HMM1        = $23
HMBL        = $24
VDELP0      = $25
VDELP1      = $26
VDELBL      = $27
RESMP0      = $28
RESMP1      = $29
HMOVE       = $2A
HMCLR       = $2B
CXCLR       = $2C

; --- READ ADDRESSES ---

CXM0P       = $00
CXM1P       = $01
CXP0FB      = $02
CXP1FB      = $03
CXM0FB      = $04
CXM1FB      = $05
CXBLPF      = $06
CXPPMM      = $07

; --- PADDLE INPUT PORTS ---

INPT0       = $08
INPT1       = $09
INPT2       = $0A
INPT3       = $0B
INPT4       = $0C
INPT5       = $0D

; --- PIA AND TIMER (6532) LOCATIONS ---

SWCHA       = $0280
CTLSWA      = $0281
SWCHB       = $0282
CTLSWB      = $0283
INTIM       = $0284
TIM1T       = $0294
TIM8T       = $0295
TIM64T      = $0296
T1024T      = $0297
