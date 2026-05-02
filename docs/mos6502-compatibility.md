# MOS 6502 compatibility status

## Scope
This CPU implements the documented/official MOS 6502 opcode set.

## Current status
- Official opcodes: implemented
- Addressing modes: implemented
- Decimal mode: basic tested, still requires broader reference validation
- Interrupts: implemented at functional level
- Cycle counting: instruction-cycle model, not full bus-cycle model
- Undocumented opcodes: not implemented

## Not yet C64-ready
This CPU is not yet a complete Commodore 64 CPU core because many C64 programs and demos may rely on undocumented 6502/6510 opcodes and cycle-sensitive behavior.

## Required before C64 integration
- reference test suite execution
- undocumented opcode policy
- 6510 I/O port behavior
- cycle-sensitive validation with VIC-II/CIA integration later
