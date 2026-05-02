# MOS 6502 compatibility status

## Scope
This CPU implements the documented/official MOS 6502 opcode set.

## Status levels

### Status A: MOS 6502 official — candidate complete
- Official opcodes: implemented and recognized by dispatcher
- Addressing modes: implemented and tested (zp, zp,X, abs, abs,X, abs,Y, indirect, (zp,X), (zp),Y, relative)
- Decimal mode: BCD matrix smoke tests pass (ADC + SBC 10 pairs each)
- Interrupts: BRK, IRQ, NMI, RTI tested at functional level
- Flags: all flags verified across instruction groups
- Cycle counting: instruction-cycle model with branch/page-cross penalties
- Cycle consistency: Step() return value matches CycleCount delta

### Status B: Reference validated — pending
- Reference ROM runner infrastructure: implemented
- `6502_functional_test.bin`: not yet validated locally
- `decimal_test.bin`: not yet validated locally

### Status C: C64/6510 ready — not ready
- Undocumented opcodes: not implemented
- 6510 I/O port ($0000/$0001): not implemented
- Cycle/bus timing model: instruction-cycle only, not bus-cycle accurate
- VIC-II/CIA integration: not started

## Required before C64 integration
- run Klaus Dormann functional test locally until all test items pass
- implement undocumented opcode policy (minimum: LAX, SAX, DCP, ISC, SLO, RLA, SRE, RRA)
- add 6510 I/O port behavior ($0000/$0001)
- validate cycle-sensitive behavior with VIC-II/CIA integration later
