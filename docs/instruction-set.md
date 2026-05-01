# Instruction Set

| Opcode | Mnemonic | Operand | Bytes | Cycles | Description |
|--------|----------|---------|-------|--------|-------------|
| 0x00 | NOP | - | 1 | 1 | No operation |
| 0x01 | LDA | #value | 2 | 2 | Load A with immediate value |
| 0x02 | LDA | addr | 3 | 4 | Load A from absolute address |
| 0x03 | STA | addr | 3 | 4 | Store A to absolute address |
| 0x04 | ADD | #value | 2 | 2 | Add immediate to A, sets Carry/Zero/Negative |
| 0x05 | SUB | #value | 2 | 2 | Subtract immediate from A, sets Carry/Zero/Negative |
| 0x06 | JMP | addr | 3 | 3 | Unconditional jump to address |
| 0x07 | JZ | addr | 3 | 2 | Jump if Zero flag is set |
| 0x08 | JNZ | addr | 3 | 2 | Jump if Zero flag is not set |
| 0x09 | OUT | port | 2 | 3 | Write A to I/O port (address = 0xC000 + port) |
| 0x0A | IN | port | 2 | 3 | Read from I/O port (address = 0xC000 + port) into A |
| 0x0B | CLI | - | 1 | 1 | Clear Interrupt Disable flag |
| 0x0C | SEI | - | 1 | 1 | Set Interrupt Disable flag |
| 0x0D | PUSH_A | - | 1 | 2 | Push A onto stack |
| 0x0E | POP_A | - | 1 | 2 | Pop A from stack |
| 0x0F | CALL | addr | 3 | 4 | Push return address (hi then lo) to stack and jump |
| 0x10 | RET | - | 1 | 4 | Pop return address (lo then hi) from stack and return |
| 0x11 | HLT | - | 1 | 1 | Halt CPU execution |

## CPU Registers

| Register | Size | Description |
|----------|------|-------------|
| A | 8-bit | Accumulator |
| X | 8-bit | General purpose |
| Y | 8-bit | General purpose |
| PC | 16-bit | Program counter |
| SP | 8-bit | Stack pointer (stack at 0x0100-0x01FF) |
| FLAGS | 4-bit | Zero, Carry, Negative, InterruptDisable |

## Flag Effects

| Instruction | Zero | Carry | Negative | Description |
|-------------|------|-------|----------|-------------|
| LDA | ✓ | - | ✓ | Set based on loaded value |
| ADD | ✓ | ✓ | ✓ | Carry = result > 0xFF |
| SUB | ✓ | ✓ | ✓ | Carry = result <= 0xFF |
| CLI/SEI | - | - | - | Only affects InterruptDisable |
