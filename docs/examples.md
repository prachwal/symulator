# Examples

## Blink LED

The blink.asm program demonstrates basic CPU functionality:

- Load immediate values
- Store to I/O port (LED)
- Subroutine calls (CALL/RET)
- Conditional jumps (JNZ)
- Infinite loops

### Source (examples/blink.asm)

```asm
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
```

### Running

```bash
dotnet run --project src/CmosCpu.ConsoleApp -- --program examples/blink.asm --cycles 100000
```

Expected output:
```
Loading program from examples/blink.asm
Running for 100000 cycles...
Cycle 3: LED ON
Cycle 518: LED OFF
Cycle 1033: LED ON
...
```

## Custom Programs

Write your own assembly files using the supported instruction set (see instruction-set.md) and run them:

```bash
dotnet run --project src/CmosCpu.ConsoleApp -- --program myprogram.asm --cycles 1000 --trace
```

The `--trace` flag enables detailed instruction-level logging.
