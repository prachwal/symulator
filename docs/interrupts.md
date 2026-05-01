# Interrupts

The CPU supports three interrupt sources: RESET, IRQ, and NMI.

## RESET

- Triggered by calling `cpu.Reset()`
- Reads reset vector from 0xFFFC (low byte) and 0xFFFD (high byte)
- Sets PC to the vector value
- Clears all registers
- Sets SP to 0xFF
- Sets InterruptDisable flag

## IRQ (Maskable Interrupt)

- Triggered by calling `cpu.RequestInterrupt()`
- Only fires if InterruptDisable flag is clear
- Sequence:
  1. Push PC high byte to stack
  2. Push PC low byte to stack
  3. Push flags to stack
  4. Set InterruptDisable flag
  5. Read vector from 0xFFFA (low) and 0xFFFB (high)
  6. Set PC to vector value
- Total cycle cost: 5 cycles

## NMI (Non-Maskable Interrupt)

- Triggered by calling `cpu.RequestNmi()`
- Cannot be masked by InterruptDisable
- Same sequence as IRQ but reads vector from 0xFFFE-0xFFFF

## Interrupt Handlers

Interrupt handlers are regular subroutines but should end with RET to return to the interrupted code. The flags pushed onto the stack are discarded in the current implementation.
