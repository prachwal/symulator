; hello-lcd-i2c.asm — writes 'A' to LCD via I2C PCF8574 backpack (4-bit mode)
; I2C controller at $FE30-$FE33, PCF8574 at address $27
; PCF8574 output byte: bits 7-4 = data nibble, bit 3 = BL, bit 2 = E, bit 1 = RW, bit 0 = RS
.org $0100

PCF_ADDR = $27
BL = $08         ; backlight bit
RS_CMD = $00     ; RS=0 for command
RS_DAT = $01     ; RS=1 for data
E_HIGH = $04     ; E pulse high

I2C_CTRL = $FE30
I2C_ADDR = $FE31
I2C_DATA = $FE32

; Helper: write byte to PCF8574 via I2C
write_pcf:
    STA I2C_DATA
    LDA #$04
    STA I2C_CTRL
    RTS

; Short delay subroutine
short_delay:
    LDA #$60
    STA_ZP $80
delay_loop:
    DEC $0080
    JNZ delay_loop
    RTS

start:
    ; Set PCF8574 I2C address
    LDA #PCF_ADDR
    STA I2C_ADDR

    ; === Send command $28: function set (4-bit, 2 lines) ===
    ; High nibble $2, RS=0: nibble=$20, BL=$08 → base=$28
    LDA #$2C        ; base | E_HIGH = $28 | $04
    JSR write_pcf
    LDA #$28        ; base (E low)
    JSR write_pcf
    ; Low nibble $8, RS=0: nibble=$80, BL=$08 → base=$88
    LDA #$8C        ; base | E_HIGH
    JSR write_pcf
    LDA #$88        ; base (E low)
    JSR write_pcf
    JSR short_delay

    ; === Send command $0C: display on, cursor off ===
    ; High nibble $0, RS=0: nibble=$00, BL=$08 → base=$08
    LDA #$0C        ; base | E_HIGH
    JSR write_pcf
    LDA #$08        ; base (E low)
    JSR write_pcf
    ; Low nibble $C, RS=0: nibble=$C0, BL=$08 → base=$C8
    LDA #$CC        ; base | E_HIGH
    JSR write_pcf
    LDA #$C8        ; base (E low)
    JSR write_pcf
    JSR short_delay

    ; === Send command $06: entry mode ===
    ; High nibble $0, RS=0: base=$08
    LDA #$0C
    JSR write_pcf
    LDA #$08
    JSR write_pcf
    ; Low nibble $6, RS=0: nibble=$60, BL=$08 → base=$68
    LDA #$6C        ; base | E_HIGH
    JSR write_pcf
    LDA #$68        ; base (E low)
    JSR write_pcf
    JSR short_delay

    ; === Send command $01: clear display ===
    ; High nibble $0, RS=0: base=$08
    LDA #$0C
    JSR write_pcf
    LDA #$08
    JSR write_pcf
    ; Low nibble $1, RS=0: nibble=$10, BL=$08 → base=$18
    LDA #$1C        ; base | E_HIGH
    JSR write_pcf
    LDA #$18        ; base (E low)
    JSR write_pcf
    JSR short_delay

    ; === Write data 'A' ($41), RS=1 ===
    ; High nibble $4, RS=1: nibble=$40, BL=$08, RS=$01 → base=$49
    LDA #$4D        ; base | E_HIGH = $49 | $04
    JSR write_pcf
    LDA #$49        ; base (E low)
    JSR write_pcf
    ; Low nibble $1, RS=1: nibble=$10, BL=$08, RS=$01 → base=$19
    LDA #$1D        ; base | E_HIGH = $19 | $04
    JSR write_pcf
    LDA #$19        ; base (E low)
    JSR write_pcf
    JSR short_delay

    HLT
