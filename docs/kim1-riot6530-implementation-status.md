# KIM-1 RIOT 6530 Implementation Status

## Overview

Two 6530 Multi-Function I/O devices are used in the KIM-1:

| Chip | Address | ROM | Role |
|------|---------|-----|------|
| 6530-002 | 0x1700-0x173F | 0x1C00-0x1FFF | LED display (Port B), keypad column scanning (Port A), timer, IRQ |
| 6530-003 | 0x1400-0x143F | 0x1800-0x1BFF | Keypad row input (Port A), cassette/RS-232 (Port B), timer, IRQ |

## Register Map (within the 64-byte I/O page, offset from base address)

| Offset | Register | Access | Description |
|--------|----------|--------|-------------|
| 0x00 | Port A Data | R/W | Port A output latch; read returns output⊕input mixed by DDR |
| 0x01 | Port A DDR | R/W | Data direction: 1=output, 0=input |
| 0x02 | Port B Data | R/W | Port B output latch |
| 0x03 | Port B DDR | R/W | Data direction for Port B |
| 0x04 | Timer /1 | Read = counter low; Write = load timer + prescaler /1 |
| 0x05 | Timer /8 | Same low-byte read; Write = load timer + prescaler /8 |
| 0x06 | Timer /64 | Same low-byte read; Write = load timer + prescaler /64 |
| 0x07 | Timer /1024 | Same low-byte read; Write = load timer + prescaler /1024 |
| 0x08 | Timer + IRQ enable | Write = load timer /1 + enable IRQ on underflow |
| 0x09 | IRQ disable | Write = disable timer IRQ |

Reading offset 0x05 or 0x06 returns: `(timer_counter >> 8) & 0x7F | (underflow ? 0x80 : 0x00)` and clears underflow + IRQ flags.

Internal RAM is mapped at offsets 0x40-0xBF (128 bytes per 6530 chip).

## Port I/O Behavior

- DDR bit = 1 → bit is output; read returns latch value
- DDR bit = 0 → bit is input; read returns external input value (set via `SetPortAInput()`/`SetPortBInput()`)
- Write to data register always updates the output latch
- Read of port register combines output latch bits (DDR=1) with external input bits (DDR=0)

## PA7 Edge Detection and IRQ

PA7 (Port A bit 7) supports configurable edge-detection IRQ:

- **Disabled by default** — `SetPortAInput()` does not trigger PA7 IRQ unless edge detection is configured
- Configurable via `ConfigurePa7Irq(enabled, fallingEdge, risingEdge)` API
- Supports independent falling-edge and rising-edge detection
- `Pa7IrqEnabled` flag gates whether PA7 IRQ contributes to the combined `IrqPending` line
- `ClearPa7Irq()` clears only the PA7 pending flag without affecting timer IRQ
- Combined IRQ line: `(_timerIrqPending && _timerIrqEnabled) || (_pa7IrqPending && _pa7IrqEnabled)`

This is a functional model, not a full datasheet-complete mapping of the 6530's edge control logic.

## Keypad Matrix

The KIM-1 keypad matrix links 6530-002 Port A (column outputs) with 6530-003 Port A (row inputs):

- Columns 0-3 driven by 6530-002 Port A bits 0-3 (active-low, DDR-controlled)
- Rows 0-5 read by 6530-003 Port A bits 0-3 (non-keypad bits preserved)
- `GetRowState(columnOutput, columnDdr)` — Only bits with DDR=1 can activate columns; multiple active columns combine rows
- `ComputerMachine.UpdateKim1KeypadMatrix()` runs on each `Step()` to propagate keypad state

## Timer Model

- Timer is decremented by `Tick(cpuCycles)` calls
- Prescaler divides cycle count before decrementing (divisors: 1, 8, 64, 1024)
- On underflow: `_timerUnderflow` flag set, `_timerIrqPending` set if `_timerIrqEnabled`
- Reading timer flag register (offset 0x05/0x06) clears underflow and timer IRQ
- New timer write clears underflow state and timer IRQ pending
- Writing to 0x09 disables timer IRQ but does not clear underflow flag
- PA7 IRQ is independent and not affected by timer flag reads

The timer model is simplified:
- No one-shot vs. continuous mode distinction (always one-shot/decrement-to-zero)
- No automatic reload after underflow
- Prescaler resolution is CPU-cycle-based, not real-time

## IRQ Integration

- `Kim1Riot6530IoDevice.IrqPending` is exposed and combines timer IRQ + PA7 IRQ
- `ComputerMachine.Step()` checks IRQ from both 6530 devices before each instruction
- CPU has `SetIrqLine(bool)` — sets a flag checked at end of `Step()`
- If I flag is clear and IRQ line active, CPU vectors through 0xFFFE
- IRQ line is re-evaluated every instruction based on device state

## Internal RAM

Each 6530 chip includes 128 bytes of internal RAM:
- 6530-002: offsets 0x40-0xBF → addresses 0x1740-0x17BF
- 6530-003: offsets 0x40-0xBF → addresses 0x1440-0x14BF
- RAM writes do not affect port/timer registers
- RAM is zero-initialized per device instance

## Diagnostic Command

`dotnet run --project src/CmosCpu.Terminal -- kim1-io --profile profiles/kim-1.json`

Shows both 6530 devices with:
- Port A/B data, DDR, external input
- Timer counter, prescaler, underflow, IRQ state
- RAM first 16 bytes
- PA7 IRQ and PA7 IRQ enabled state

## Tests

### Kim1DeviceTests (52 tests)
- Port A/B read/write with DDR (mixed input/output)
- Version tracking
- Timer: all 4 prescalers, underflow, IRQ enable/disable
- PA7: configurable edge detect, default disabled, falling/rising/both
- PA7 IRQ: ClearPa7Irq, timer flag read independence, combined ClearIrq
- Keypad: press/release, valid key filtering, matrix scanning
- Keypad matrix: DDR semantics, multiple active columns, no active column
- Internal RAM: separate read/write for 002 and 003, no port interference
- Computer-machine integration: keypad scan propagation, bit preservation
- Profile loading

## Known Limitations

1. **No auto-reload** — Real 6530 timer can be configured for continuous (auto-reload) mode. Current model only supports one-shot.
2. **PA7 edge model simplified** — PA7 edge detection uses a functional `ConfigurePa7Irq()` API rather than full register-level control. Real 6530 edge detect is controlled through port configuration registers.
3. **KIM-1 monitor** — The monitor may still not fully function because it requires precise keypad scanning logic timing and IRQ handling.
4. **IRQ timing** — IRQ is checked at end of each instruction; real 6502 checks at end of current instruction, which is equivalent for most cases.
