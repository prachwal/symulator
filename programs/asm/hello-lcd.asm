; hello-lcd.asm — writes "Hello World!" to 16x2 LCD via direct MMIO
; LCD_CMD = $FE00, LCD_DATA = $FE01
.org $0100

LCD_CMD = $FE00
LCD_DATA = $FE01

start:
    ; Init LCD in 8-bit mode (function set: 8-bit, 2 lines, 5x8 font)
    LDA #$38
    STA LCD_CMD

    ; Display on, cursor off, blink off
    LDA #$0C
    STA LCD_CMD

    ; Entry mode: increment, no shift
    LDA #$06
    STA LCD_CMD

    ; Clear display
    LDA #$01
    STA LCD_CMD

    ; Write "Hello World!"
    LDA #'H'
    STA LCD_DATA
    LDA #'e'
    STA LCD_DATA
    LDA #'l'
    STA LCD_DATA
    LDA #'l'
    STA LCD_DATA
    LDA #'o'
    STA LCD_DATA
    LDA #' '
    STA LCD_DATA
    LDA #'W'
    STA LCD_DATA
    LDA #'o'
    STA LCD_DATA
    LDA #'r'
    STA LCD_DATA
    LDA #'l'
    STA LCD_DATA
    LDA #'d'
    STA LCD_DATA
    LDA #'!'
    STA LCD_DATA

    HLT
