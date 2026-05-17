# Universal 6502 Assembly Guidelines (DASM & MADS)

The Lokey-YM SDK supports both **DASM** (the community standard) and **MADS** (for advanced development). To maintain a single source of truth for core logic, we use a "Universal Subset" of 6502 assembly combined with conditional blocks.

Here are the guidelines for writing dual-compatible code.

## 1. The `MADS` Define
The primary mechanism for compatibility is a conditional block based on a `MADS` variable. This variable must be defined on the command line during assembly.

**Compilation Commands:**
*   **DASM:** `dasm source.asm -DMADS=0 -f3 -osource.bin`
*   **MADS:** `mads source.asm -d:MADS=1 -o:source.bin`

## 2. Setup and Includes
Because DASM and MADS have different ways of handling includes (`include` vs `icl`) and memory mapping (`org` vs `blk none`), every universal file should start with a setup block:

```assembly
    .if MADS
      opt h- f+        ; No DOS header, raw binary
      icl "maria.inc"
      icl "ym2149.inc"
      blk none $8000   ; Map to $8000
    .else
      processor 6502
      include "maria.inc"
      include "ym2149.inc"
      org $8000
    .endif
```

## 3. Universal Data Directives
Both assemblers support dot-prefixed directives for raw data. Avoid assembler-specific shortcuts.

*   ❌ **Don't use:** `dc.b`, `dc.w`, `dta b()`, `dta a()`
*   ✅ **Do use:** `.byte`, `.word`

```assembly
note_table_lo:
    .byte <428, <381, <339
note_table_hi:
    .byte >428, >381, >339
```

## 4. Labels and Scopes
DASM requires all labels and equates to start in the **first column**. MADS is more flexible but will accept column-1 labels.

*   ✅ **Always** start your labels and equates in column 1.
*   ❌ **Don't use** `.proc`, `.zpvar`, or `.local` in universal code, as DASM does not support them.

## 5. Local Labels
Both assemblers support local labels, but the syntax differs. To remain universal, avoid the dot-prefix (`.loop`) and use unique names or underscore prefixes if supported by your specific versions.
*   **Recommended**: Use descriptive unique names (e.g., `_wait_vbi`) rather than relying on local-scope markers.

## 6. ROM Padding & Vectors
Atari 7800 ROMs must be exactly 32,768 bytes with vectors at the very end. Use a conditional block to handle the padding difference:

```assembly
    .if MADS
        .align $fff8, $ff
        .byte $ff, $83  ; Footer/Cart Type
        .word reset, reset, reset
    .else
        org $fff8, $ff
        .byte $ff, $83
        .word reset
        .word reset
        .word reset
    .endif
```

## 7. Instruction Set
Stick to the standard 6502 instruction set.

*   ❌ **Don't use:** `mva`, `adw`, `#if`, `#while` (MADS specific macros)
*   ✅ **Do use:** `lda`, `sta`, `cmp`, `bne`

## Summary: A Universal Template
```assembly
; ============================================================
; template.asm -- Universal 6502 Source
; ============================================================

    .if MADS
      opt h- f+
      icl "maria.inc"
      blk none $8000
    .else
      processor 6502
      include "maria.inc"
      org $8000
    .endif

reset:
    sei
    cld
    ldx #$ff
    txs

main_loop:
    ; Wait for VSync
v1: bit MSTAT
    bmi v1
v2: bit MSTAT
    bpl v2
    jmp main_loop

    ; --- Footer ---
    .if MADS
        .align $fff8, $ff
        .byte $ff, $83
        .word reset, reset, reset
    .else
        org $fff8, $ff
        .byte $ff, $83
        .word reset, reset, reset
    .endif
```
