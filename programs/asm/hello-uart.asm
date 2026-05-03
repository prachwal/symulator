; hello-uart.asm — writes "Hello UART" to UART terminal
; UART_DATA = $FE40
.org $0100

UART_DATA = $FE40

start:
    LDA #'H'
    STA UART_DATA
    LDA #'e'
    STA UART_DATA
    LDA #'l'
    STA UART_DATA
    LDA #'l'
    STA UART_DATA
    LDA #'o'
    STA UART_DATA
    LDA #' '
    STA UART_DATA
    LDA #'U'
    STA UART_DATA
    LDA #'A'
    STA UART_DATA
    LDA #'R'
    STA UART_DATA
    LDA #'T'
    STA UART_DATA
    LDA #$0D
    STA UART_DATA
    LDA #$0A
    STA UART_DATA
    HLT
