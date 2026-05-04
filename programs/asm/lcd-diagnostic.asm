; lcd-diagnostic.asm
; Minimal LCD test: initialize and write one character.
; If LCD shows 'X', the LCD write path works.

LCD_CMD  = $FE00
LCD_DATA = $FE01

.org $0100

start:
    LDA #$38       ; function set
    STA LCD_CMD

    LDA #$0C       ; display on
    STA LCD_CMD

    LDA #$01       ; clear
    STA LCD_CMD

    LDA #$80       ; DDRAM addr line 1 col 0
    STA LCD_CMD

    LDA #'X'       ; write character
    STA LCD_DATA

done:
    HLT
