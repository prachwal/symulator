# CmosCpu Simulator

Educational 8-bit CPU simulator inspired by CMOS/TTL-based computers. Built with .NET 10, C#, clean architecture, and modular design.

## Features

- 8-bit CPU with 16-bit address bus
- Minimal instruction set (17 instructions)
- Memory-mapped I/O devices (LED, Timer, RTC)
- Interrupt handling (IRQ, NMI, RESET)
- Text assembler with label support
- Step and continuous execution modes with cycle-accurate device ticking
- DI-friendly architecture with NLog logging
- Universal machine abstraction (`IMachine`, `IMachineProfile`, `ICpuCore`)
- `CpuStepResult` with per-instruction cycle count
- `IDebugger` with breakpoint support (stop-on-breakpoint during Run)
- Blazor diagnostic frontend (Machine Status, Memory Map, Settings, Debugger pages)
- 140+ unit and integration tests

## Project Structure

```
CmosCpuSimulator/
├─ src/
│  ├─ CmosCpu.Core/          Core interfaces, types, and emulator contracts
│  ├─ CmosCpu.Bus/           System bus implementation (IBus)
│  ├─ CmosCpu.Memory/        RAM, ROM, MemoryMap
│  ├─ CmosCpu.Cpu/           CPU core with instruction set
│  ├─ CmosCpu.Devices/       I/O devices (LED, Timer, RTC)
│  ├─ CmosCpu.Assembler/     Text assembler
│  ├─ CmosCpu.Runtime/       DI container, Machine, MachineBuilder, profile, adapter
│  ├─ CmosCpu.ConsoleApp/    CLI application
│  └─ CmosCpu.BlazorApp/     Blazor Server diagnostic frontend
├─ tests/                    Unit and integration tests (MSTest + Moq + FluentAssertions)
├─ docs/                     Documentation
├─ examples/                 Example programs
└─ README.md
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or .NET 8+)
- Linux, macOS, or Windows

## Build

```bash
dotnet build
```

## Run Tests

```bash
dotnet test
```

## Run Console App

Run the default blink program (10000 cycles):

```bash
dotnet run --project src/CmosCpu.ConsoleApp
```

Run with a custom program:

```bash
dotnet run --project src/CmosCpu.ConsoleApp -- --program examples/blink.asm --cycles 100000
```

With trace logging (instruction-level debug):

```bash
dotnet run --project src/CmosCpu.ConsoleApp -- --program examples/blink.asm --cycles 1000 --trace
```

## Run Blazor Frontend

```bash
dotnet run --project src/CmosCpu.BlazorApp
```

Navigate to `http://localhost:5113` in your browser.

## Emulator Contracts

The solution introduces a universal emulator architecture for future multi-CPU support:

| Interface | Purpose |
|-----------|---------|
| `ICpuCore` | CPU abstraction (steps, interrupts, identity) |
| `IClockedDevice` | Device that ticks with the system clock |
| `IResettable` | Object that can be reset |
| `IMachine` | Complete machine (CPU + bus + devices) |
| `IMachineProfile` | Named machine configuration profile |
| `IMachineBuilder` | Builder for assembling a machine |
| `IMemoryMap` | Memory region lookup |
| `IDebugger` | Breakpoint management |
| `IEmulatorUiSettings` | Runtime/UI settings (log toggles, limits) |
| `CpuStepResult` | Per-instruction result (PC, cycles) |

See [docs/architecture.md](docs/architecture.md) for detailed architecture documentation.

## Example: Blink LED

The `examples/blink.asm` program toggles an LED at address 0xC000:

```asm
.org 0x8000

start:
    LDA #0x01          ; ON
    STA 0xC000         ; write to LED
    CALL delay
    LDA #0x00          ; OFF
    STA 0xC000
    CALL delay
    JMP start          ; repeat

delay:
    LDA #0xFF
loop:
    SUB #0x01
    JNZ loop
    RET
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

## Architecture

See [docs/architecture.md](docs/architecture.md) for detailed architecture documentation.

## Instruction Set

See [docs/instruction-set.md](docs/instruction-set.md) for the complete instruction reference.

### Quick Reference

| Opcode | Mnemonic | Bytes | Description |
|--------|----------|-------|-------------|
| 0x00 | NOP | 1 | No operation |
| 0x01 | LDA #v | 2 | Load A immediate |
| 0x02 | LDA a | 3 | Load A absolute |
| 0x03 | STA a | 3 | Store A absolute |
| 0x04 | ADD #v | 2 | Add to A |
| 0x05 | SUB #v | 2 | Subtract from A |
| 0x06 | JMP a | 3 | Jump |
| 0x07 | JZ a | 3 | Jump if Zero |
| 0x08 | JNZ a | 3 | Jump if not Zero |
| 0x09 | OUT p | 2 | Write A to I/O port |
| 0x0A | IN p | 2 | Read from I/O port |
| 0x0B | CLI | 1 | Clear interrupts |
| 0x0C | SEI | 1 | Set interrupts |
| 0x0D | PUSH_A | 1 | Push A to stack |
| 0x0E | POP_A | 1 | Pop A from stack |
| 0x0F | CALL a | 3 | Call subroutine |
| 0x10 | RET | 1 | Return from subroutine |
| 0x11 | HLT | 1 | Halt CPU |

## Memory Map

| Range | Description |
|-------|-------------|
| 0x0000 - 0x7FFF | RAM |
| 0x8000 - 0xBFFF | ROM |
| 0xC000 - 0xC0FF | I/O devices |
| 0xFF00 - 0xFFFF | System vectors |

## Platform Note

The solution targets **net10.0**. WPF has been removed; the new UI direction is **Blazor Server**.
