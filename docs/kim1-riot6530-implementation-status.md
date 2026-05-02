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

## Port I/O Behavior

- DDR bit = 1 → bit is output; read returns latch value
- DDR bit = 0 → bit is input; read returns external input value (set via `SetPortAInput()`/`SetPortBInput()`)
- Write to data register always updates the output latch

## Timer Model

- Timer is decremented by `Tick(cpuCycles)` calls
- Prescaler divides cycle count before decrementing (divisors: 1, 8, 64, 1024)
- On underflow: `_timerUnderflow` flag set, `_irqPending` set if IRQ enabled
- Reading timer flag register (offset 0x05/0x06) clears underflow and IRQ
- New timer write clears underflow state

The timer model is simplified:
- No one-shot vs. continuous mode distinction (always one-shot/decrement-to-zero)
- No automatic reload after underflow
- Prescaler resolution is CPU-cycle-based, not real-time

## IRQ Integration

- `Kim1Riot6530IoDevice.IrqPending` is exposed
- `ComputerMachine.Step()` checks IRQ from both 6530 devices before each instruction
- CPU has `SetIrqLine(bool)` — sets a flag checked at end of `Step()`
- If I flag is clear and IRQ line active, CPU vectors through 0xFFFE
- IRQ line is re-evaluated every instruction based on device state

## CPU Changes

- `Mos6502Cpu.SetIrqLine(bool)` added
- `Mos6502Cpu.Step()` checks IRQ at end of instruction execution
- `Mos6502Cpu.Reset()` clears IRQ line

## Changes to Existing Classes

### `ComputerMachine`
- Added `Kim1Riot003` property for second 6530-003 device
- Updated `Step()` to tick both RIOTs and check IRQ from both
- Updated `MapDevice()` to handle `"kim1-6530-003-io"` type

### `profiles/kim-1.json`
- Split single RIOT device into two: `kim1-6530-002` (0x1700) and `kim1-6530-003` (0x1400)
- Added device descriptions matching KIM-1 hardware

### `Kim1Riot6530IoDevice`
- Complete rewrite: ports, DDR, timer with 4 prescalers, IRQ pending/enable
- Previous flat register array replaced with explicit register model

## Diagnostic Command

`dotnet run --project src/CmosCpu.Terminal -- kim1-io --profile profiles/kim-1.json`

Shows both 6530 devices with:
- Port A/B data, DDR, external input
- Timer counter, prescaler, underflow, IRQ state

## Tests

### Kim1DeviceTests (24 tests)
- `Kim1Io_Handles_1700_To_17FF`
- `Kim1Io_PortA_WithDDR_ReturnsWrittenValue`
- `Kim1Io_DDR_RegisterRoundtrip`
- `Kim1Io_Version_Increments_OnWriteChange`
- `Kim1Io_PortA_InputBitsComeFromExternalInput`
- `Kim1Io_PortB_WithDDR_ReturnsWrittenValue`
- `Kim1Io_PortB_MixedDDR`
- `Kim1Io_TimerWritePrescaler1_TicksCorrectly`
- `Kim1Io_TimerPrescaler8_TicksCorrectly`
- `Kim1Io_TimerPrescaler64_TicksCorrectly`
- `Kim1Io_TimerPrescaler1024_TicksCorrectly`
- `Kim1Io_TimerUnderflow_SetsIrqPending_WhenIrqEnabled`
- `Kim1Io_TimerUnderflow_DoesNotSetIrqPending_WhenIrqDisabled`
- `Kim1Io_ReadTimerFlagRegister_ClearsUnderflowAndIrq`
- `Kim1Io_TimerWrite_AfterUnderflow_RestartsTimer`
- `Kim1Io_IrqEnable_WriteTo08_EnablesIrq`
- `Kim1Io_IrqDisable_WriteTo09_DisablesIrq`
- `ComputerMachine_MapsKim1IoRange`
- `Kim1Profile_LoadsWithoutUnknownDeviceException`
- `Kim1Keypad_PressKey_AddsKey`
- `Kim1Keypad_ReleaseKey_RemovesKey`
- `Kim1LedDisplay_DefaultState_IsSixDashes`
- `Kim1Keypad_OnlyValidKeysAccepted`
- `Kim1LedDisplay_Update_ChangesDigits`

### Integration (Terminal tests, 70 total)
- All existing profile boot tests pass
- New `kim1-io` command tested through router

## Known Limitations

1. **No auto-reload** — Real 6530 timer can be configured for continuous (auto-reload) mode. Current model only supports one-shot.
2. **No PA7 edge detect** — Real 6530 has edge detection on PA7 for keypad interrupt. Not implemented.
3. **No 6530 RAM** — Each 6530 has 128 bytes of internal RAM (0x1740-0x17BF for 002, 0x1440-0x14BF for 003). Not currently mapped.
4. **KIM-1 monitor** — The monitor may still not fully function because it expects keypad scanning logic (column strobe + row read) which requires port input/output coordination between the two RIOTs.
5. **IRQ timing** — IRQ is checked at end of each instruction; real 6502 checks at end of current instruction, which is equivalent for most cases.
