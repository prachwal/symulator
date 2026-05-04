; rtc-clock.asm
; Real-time clock reading RTC DS3231 via direct bus and displaying on LCD 16x2.
; Writes "HH:MM:SS" on line 2, updates in an infinite loop.
;
; Hardware: Minimal Blink CPU + RTC DS3231 (rtc-mmio, linear mode at 0xD100)
;           + HD44780 LCD 16x2 (hd44780-mmio at 0xFE00)

RTC_SEC  = $D100   ; RTC seconds register (linear mode)
RTC_MIN  = $D101   ; RTC minutes register
RTC_HOUR = $D102   ; RTC hours register
LCD_CMD  = $FE00   ; LCD command register
LCD_DATA = $FE01   ; LCD data register

.org $0100

; ── Initialize LCD ──────────────────────────────────────────────
start:
    LDA #$38       ; function set: 8-bit, 2 lines, 5x8 font
    STA LCD_CMD
    LDA #$0C       ; display on, cursor off, blink off
    STA LCD_CMD
    LDA #$01       ; clear display
    STA LCD_CMD
    LDA #$06       ; entry mode: increment, no shift
    STA LCD_CMD

    ; Write static header on line 1
    LDA #$80       ; set DDRAM addr to line 1, col 0
    STA LCD_CMD
    LDA #'R'
    STA LCD_DATA
    LDA #'T'
    STA LCD_DATA
    LDA #'C'
    STA LCD_DATA
    LDA #' '
    STA LCD_DATA
    LDA #'C'
    STA LCD_DATA
    LDA #'l'
    STA LCD_DATA
    LDA #'o'
    STA LCD_DATA
    LDA #'c'
    STA LCD_DATA
    LDA #'k'
    STA LCD_DATA

; ── Main loop: read RTC, convert BCD, write to LCD line 2 ──────
; Memory map for time buffers:
;   $F4 = hours tens, $F5 = hours ones
;   $F6 = minutes tens, $F7 = minutes ones
;   $F8 = seconds tens, $F9 = seconds ones
;   $F2/$F3 = bcd_to_ascii temporary output
; ────────────────────────────────────────────────────────────────
main_loop:
    ; Read hours
    LDA_ABS RTC_HOUR
    CALL bcd_to_ascii  ; → $F2 = tens ASCII, $F3 = ones ASCII
    LDA $F2
    STA $F4            ; hours tens
    LDA $F3
    STA $F5            ; hours ones

    ; Read minutes
    LDA_ABS RTC_MIN
    CALL bcd_to_ascii
    LDA $F2
    STA $F6            ; minutes tens
    LDA $F3
    STA $F7            ; minutes ones

    ; Read seconds
    LDA_ABS RTC_SEC
    CALL bcd_to_ascii
    LDA $F2
    STA $F8            ; seconds tens
    LDA $F3
    STA $F9            ; seconds ones

    ; Write to LCD line 2
    LDA #$C0           ; set DDRAM addr to line 2, col 0
    STA LCD_CMD

    LDA $F4
    STA LCD_DATA
    LDA $F5
    STA LCD_DATA
    LDA #':'
    STA LCD_DATA

    LDA $F6
    STA LCD_DATA
    LDA $F7
    STA LCD_DATA
    LDA #':'
    STA LCD_DATA

    LDA $F8
    STA LCD_DATA
    LDA $F9
    STA LCD_DATA

    JMP main_loop

; ── BCD to ASCII conversion ─────────────────────────────────────
; Input:  A = BCD byte (e.g., 0x45)
; Output: $F2 = tens digit ASCII (e.g., '4'), $F3 = ones digit ASCII (e.g., '5')
; Uses:   $F0 (temp), $F1 (input copy)
; ────────────────────────────────────────────────────────────────
bcd_to_ascii:
    STA $F1            ; save input

    ; --- Convert tens digit ---
    AND #$F0           ; isolate high nibble (e.g., 0x40)
    STA $F0            ; value to reduce

    LDA #$00
    STA $F2            ; tens counter

tens_loop:
    LDA $F0
    JNZ do_tens_sub
    JMP tens_done
do_tens_sub:
    SUB #$10           ; subtract 16
    STA $F0
    LDA $F2
    ADD #$01           ; increment counter
    STA $F2
    JMP tens_loop

tens_done:
    LDA $F2
    ADD #$30           ; digit → ASCII
    STA $F2

    ; --- Convert ones digit ---
    LDA $F1
    AND #$0F           ; isolate low nibble
    ADD #$30           ; digit → ASCII
    STA $F3

    RET
