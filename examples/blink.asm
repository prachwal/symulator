; Blink LED program for CmosCpu Simulator
; Toggles LED at 0xC000 ON and OFF in an infinite loop

.org 0x8000

start:
    LDA #0x01          ; Load 1 (ON) into accumulator
    STA 0xC000         ; Write to LED device at port 0xC000
    CALL delay         ; Wait

    LDA #0x00          ; Load 0 (OFF) into accumulator
    STA 0xC000         ; Write to LED device
    CALL delay         ; Wait

    JMP start          ; Repeat forever

delay:
    LDA #0xFF          ; Load delay counter
loop:
    SUB #0x01          ; Decrement
    JNZ loop           ; Loop if not zero
    RET                ; Return from subroutine
