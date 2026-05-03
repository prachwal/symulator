; i2c-scan.asm — scans I2C bus 0x01-0x7F via PCF8574
; outputs found addresses to UART terminal
.org $0100

I2C_ADDRESS = $FE31
I2C_DATA    = $FE32
I2C_CTRL    = $FE30
I2C_STATUS  = $FE33
UART_DATA   = $FE40
UART_STATUS = $FE41

start:
    ; "I2C Scan\r"
    LDA #'I'
    STA UART_DATA
    LDA #'2'
    STA UART_DATA
    LDA #'C'
    STA UART_DATA
    LDA #' '
    STA UART_DATA
    LDA #'S'
    STA UART_DATA
    LDA #'c'
    STA UART_DATA
    LDA #'a'
    STA UART_DATA
    LDA #'n'
    STA UART_DATA
    LDA #$0D
    STA UART_DATA

    ; counter = 127
    LDA #$7F
    STA_ZP $F0

loop:
    LDA_ZP $F0
    STA I2C_ADDRESS
    LDA #$00
    STA I2C_DATA
    LDA #$04
    STA I2C_CTRL

    LDA I2C_STATUS
    AND #$01
    BNE skip

    ; found: output byte + space
    LDA_ZP $F0
    STA UART_DATA
    LDA #' '
    STA UART_DATA

skip:
    DEC $00F0
    JNZ loop

    ; "\rDone\r"
    LDA #$0D
    STA UART_DATA
    LDA #'D'
    STA UART_DATA
    LDA #'o'
    STA UART_DATA
    LDA #'n'
    STA UART_DATA
    LDA #'e'
    STA UART_DATA
    LDA #$0D
    STA UART_DATA

    HLT
