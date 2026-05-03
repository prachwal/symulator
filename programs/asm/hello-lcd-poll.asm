; hello-lcd-poll.asm — writes "Hello World!" to 16x2 LCD with busy-flag polling
; LCD_CMD = $FE00, LCD_DATA = $FE01
.org $0100

LCD_CMD = $FE00
LCD_DATA = $FE01

start:
    ; Init LCD in 8-bit mode
    LDA #$38
    STA LCD_CMD
    JSR wait_lcd

    ; Display on, cursor off
    LDA #$0C
    STA LCD_CMD
    JSR wait_lcd

    ; Entry mode: increment, no shift
    LDA #$06
    STA LCD_CMD
    JSR wait_lcd

    ; Clear display
    LDA #$01
    STA LCD_CMD
    JSR wait_lcd

    ; Write "Hello World!"
    LDA #'H'
    STA LCD_DATA
    JSR wait_lcd
    LDA #'e'
    STA LCD_DATA
    JSR wait_lcd
    LDA #'l'
    STA LCD_DATA
    JSR wait_lcd
    LDA #'l'
    STA LCD_DATA
    JSR wait_lcd
    LDA #'o'
    STA LCD_DATA
    JSR wait_lcd
    LDA #' '
    STA LCD_DATA
    JSR wait_lcd
    LDA #'W'
    STA LCD_DATA
    JSR wait_lcd
    LDA #'o'
    STA LCD_DATA
    JSR wait_lcd
    LDA #'r'
    STA LCD_DATA
    JSR wait_lcd
    LDA #'l'
    STA LCD_DATA
    JSR wait_lcd
    LDA #'d'
    STA LCD_DATA
    JSR wait_lcd
    LDA #'!'
    STA LCD_DATA
    JSR wait_lcd

    HLT

wait_lcd:
    LDA LCD_CMD
    AND #$80
    BNE wait_lcd
    RTS
