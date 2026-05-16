; ============================================================
; dasm_compat.inc -- DASM Transition Layer for MADS
; ============================================================
; This file helps bridge the gap between DASM and MADS.
; Usage: mads -m:dasm_compat.inc source.asm
; ============================================================

; --- 1. Processor Mapping ---
.macro processor
    ; DASM: processor 6502
    .if :1 == 6502
        opt c-
    .endif
.endm

; --- 2. Include Mapping ---
.macro include
    ; DASM: include "file.asm"
    icl :1
.endm

; --- 3. Segment Mapping ---
.macro seg
    ; DASM: SEG ROM (Informational only in 7800 projects)
.endm

; --- 4. Transition Notes for DASM Users ---
;
; A. Conditionals:
;    DASM: if / endif / else
;    MADS: .if / .endif / .else (Native, no macro needed)
;
; B. Constants & Definitions:
;    DASM: label SET value
;    MADS: label SET value (Native, compatible)
;
; C. Hex Numbers:
;    DASM: $FFFF
;    MADS: $FFFF (Native, compatible)
;
; D. Data Directives (The Manual Step):
;    DASM 'dc.b' and 'dc.w' cannot be macros because they contain dots.
;    Recommended Search & Replace:
;    dc.b  => dta b(
;    dc.w  => dta a(
;    (And add the closing parenthesis)
;
; E. ORG Padding:
;    DASM: org $FFF8,0
;    MADS: .align $FFF8, 0 ( cleaner / modern )
;
; F. Header Guard Pattern:
;    The DASM 'ifnconst' macro is tricky due to block closure. 
;    We recommend replacing the DASM pattern:
;       ifnconst build_with_header
;    with the MADS equivalent:
;       .if .not .def build_with_header
; ============================================================
