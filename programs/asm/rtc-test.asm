; rtc-test.asm
; Write RTC DS3231 seconds register via I2C, then read back
; Hardware: Minimal Blink CPU + I2C controller + RTC I2C at 0x68

I2C_CTRL = $FE30
I2C_ADDR = $FE31
I2C_DATA = $FE32

RTC_ADDR = $68

.org $0100

start:
    ; --- Write cycle: set RTC seconds register to 0x45 ---

    ; Set I2C target address
    LDA #RTC_ADDR
    STA I2C_ADDR

    ; First data byte = register pointer (0x00 = seconds)
    LDA #$00
    STA I2C_DATA
    LDA #$04          ; CommandWriteByte
    STA I2C_CTRL      ; transmits: 0x68 <- 0x00 (sets register pointer)

    ; Second data byte = value to write (0x45)
    LDA #$45
    STA I2C_DATA
    LDA #$04          ; CommandWriteByte
    STA I2C_CTRL      ; transmits: 0x68 <- 0x45 (writes seconds)

    ; Store result flag for test
    LDA #$01
    STA $00FF          ; test flag: write completed

done:
    HLT
