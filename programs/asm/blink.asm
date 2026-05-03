; blink.asm — toggles LED port forever
; LED_PORT = $FF00
.org $0100

LED_PORT = $FF00

start:
    LDA #$01
    STA LED_PORT

    LDA #$20
    STA_ZP $00

delay_outer:
    DEC $0000
    JNZ delay_outer

    LDA #$00
    STA LED_PORT

    LDA #$20
    STA_ZP $00

delay_outer2:
    DEC $0000
    JNZ delay_outer2

    JMP start
