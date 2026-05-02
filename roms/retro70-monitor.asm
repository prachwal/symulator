; retro70-monitor.asm
; Minimal MOS 6502 monitor ROM for Retro70 computer
; ROM origin: $F000
; Text screen: $D000
; ROM size: 4 KB
; Vectors:
;   NMI   -> $F000
;   RESET -> $F000
;   IRQ   -> $F000

        .org $F000

RESET:
        LDX #$00

PRINT:
        LDA MESSAGE,X
        BEQ HALT
        STA $D000,X
        INX
        JMP PRINT

HALT:
        JMP HALT

MESSAGE:
        .byte "RETRO70 READY", $0D, $0A, "> ", $00

        .org $FFFA
        .word RESET
        .word RESET
        .word RESET
