# Architecture

## Modules

| Module | Description |
|--------|-------------|
| CmosCpu.Core | Core interfaces (IBus, IBusDevice, ISimulator, IAssembler, ICpuCore, IMachine, IMachineProfile, IMemoryMap, IClockedDevice, IResettable) and types (CpuRegisters, Opcode, CpuFlags, MemoryRegion, InstructionInfo, SimulatorSnapshot) |
| CmosCpu.Bus | System bus implementation that routes reads/writes to registered devices |
| CmosCpu.Memory | RAM, ROM, MemoryMap |
| CmosCpu.Cpu | CPU core with fetch-decode-execute cycle and minimal ISA |
| CmosCpu.Devices | I/O devices: LedDevice, TimerDevice, RtcDevice |
| CmosCpu.Assembler | Simple text assembler with label support |
| CmosCpu.Runtime | Machine, MachineBuilder, adapter, Educational8BitMachineProfile, DI setup |
| CmosCpu.ConsoleApp | CLI application to load and run programs |
| CmosCpu.BlazorApp | Blazor Server diagnostic frontend |

## Dependencies

```
CmosCpu.Core (no deps)
CmosCpu.Bus -> CmosCpu.Core
CmosCpu.Memory -> CmosCpu.Core, CmosCpu.Bus
CmosCpu.Cpu -> CmosCpu.Core, CmosCpu.Bus
CmosCpu.Devices -> CmosCpu.Core, CmosCpu.Bus
CmosCpu.Assembler -> CmosCpu.Core
CmosCpu.Runtime -> CmosCpu.Core, CmosCpu.Bus, CmosCpu.Memory, CmosCpu.Cpu, CmosCpu.Devices, CmosCpu.Assembler
CmosCpu.ConsoleApp -> CmosCpu.Runtime
CmosCpu.BlazorApp -> CmosCpu.Runtime (plus ASP.NET Core)
```

## Emulator Contracts

The solution prepares for multi-CPU support with these core contracts:

| Interface | Purpose |
|-----------|---------|
| `ICpuCore` | CPU abstraction; extends `IClockedDevice` and `IResettable` |
| `IClockedDevice` | Device that receives a `Tick(ulong cycle)` call |
| `IResettable` | Object implementing `Reset()` |
| `IMachine` | Complete machine with CPU, bus, instruction/cycle stepping, and run loop |
| `IMachineBuilder` | Builder pattern for assembling a machine |
| `IMachineProfile` | Named configuration (e.g. "Educational 8-bit machine") |
| `IMemoryMap` | Address-to-region lookup for visualization |
| `IDebugger` | Breakpoint management |
| `CpuStepResult` | Per-instruction step result (PC before/after, opcode, cycles) |
| `IBus` | Read/write with device routing |

### ICpuCore

```csharp
public interface ICpuCore : IClockedDevice, IResettable
{
    string Name { get; }
    CpuRegisters Registers { get; }
    bool IsHalted { get; }
    void StepInstruction();
    void RequestInterrupt(InterruptType type);
}
```

### IMachine

```csharp
public interface IMachine : IResettable
{
    ICpuCore Cpu { get; }
    IBus Bus { get; }
    ulong Cycle { get; }
    bool IsRunning { get; }
    void StepInstruction();
    void StepCycle();
    void Run(ulong maxCycles);
    void Stop();
}
```

## Educational8BitMachineProfile

The `Educational8BitMachineProfile` recreates the current machine configuration:
- CPU: `CmosCpuCoreAdapter` wrapping `CpuCore`
- RAM: `0x0000 - 0x7FFF`
- ROM: `0x8000 - 0xBFFF`
- I/O: `0xC000 - 0xC0FF` (LED at 0xC000, Timer at 0xC010)
- Vectors: `0xFF00 - 0xFFFF` (reset at 0xFFFC-0xFFFD = 0x8000)

### Using the profile

```csharp
var builder = new MachineBuilder();
var profile = new Educational8BitMachineProfile();
profile.Configure(builder);
var machine = builder.Build();
machine.Reset();
machine.Run(10000);
```

With debugger:

```csharp
var dbg = new DebuggerService();
dbg.AddBreakpoint(0x8005);
builder.WithDebugger(dbg);
var machine = builder.Build();
machine.Run(10000); // stops when PC == 0x8005
```

## Cycle Timing

`Machine.Run()` executes instructions and ticks clocked devices per-instruction-cycle:

1. `ICpuCore.StepInstruction()` returns `CpuStepResult` with `Cycles` field
2. `Machine` ticks all `IClockedDevice` devices `Cycles` times
3. `Machine.Cycle` increments by `Cycles`

For the current educational CPU, cycles are computed from the difference in `CpuRegisters.CycleCount` before and after the step (each bus fetch/side-effect increments the CPU-internal cycle counter).

## Debugger

`IDebugger` provides breakpoint support:

- `AddBreakpoint(ushort address)` — adds a breakpoint
- `RemoveBreakpoint(ushort address)` — removes a breakpoint
- `ClearBreakpoints()` — clears all breakpoints
- `IsBreakpoint(ushort address)` — checks if address has a breakpoint

`Machine.Run()` checks the current PC against breakpoints before executing each instruction. If the PC matches a breakpoint, execution stops. `Machine.StepInstruction()` ignores breakpoints.

## Adapter Pattern

The existing `CpuCore` class is wrapped in `CmosCpuCoreAdapter` to implement `ICpuCore` without modifying the original CPU:

| ICpuCore method | CpuCore mapping |
|-----------------|-----------------|
| `Reset()` | `CpuCore.Reset()` |
| `StepInstruction()` | `CpuCore.Step()` |
| `RequestInterrupt(Reset)` | `CpuCore.Reset()` |
| `RequestInterrupt(Irq)` | `CpuCore.RequestInterrupt()` |
| `RequestInterrupt(Nmi)` | `CpuCore.RequestNmi()` |

## Execution Flow

1. User provides assembly source code
2. SimpleAssembler compiles it to binary
3. Binary is loaded into ROM device
4. CPU reads reset vector (0xFFFC-0xFFFD) to find entry point
5. CPU enters fetch-decode-execute loop:
   - Fetch: read byte from memory at PC address via bus
   - Decode: map opcode to instruction
   - Execute: perform operation, update registers/flags
6. Bus routes reads/writes to appropriate device based on address range

## CPU Cycle

The CPU implements a simple fetch-decode-execute cycle:

1. **Fetch**: Reads the byte at the address in the PC register from the bus, increments PC and cycle count.
2. **Decode**: The opcode byte is interpreted as an instruction.
3. **Execute**: The instruction is executed. Multi-byte instructions fetch additional operands.

## Bus Architecture

The SystemBus maintains a list of IBusDevice instances. Each device declares its address range (StartAddress to EndAddress). When a read or write occurs, the bus iterates through devices and routes the operation to the first matching device. Unmapped addresses return 0 on read and log a warning on write.

## Interrupt Handling

- **IRQ**: Triggered when RequestInterrupt() is called and the InterruptDisable flag is clear. CPU pushes PC (hi then lo) and flags to stack, sets InterruptDisable, reads vector from 0xFFFA-0xFFFB, and jumps to the handler address.
- **NMI**: Similar to IRQ but reads vector from 0xFFFE-0xFFFF and is not masked by InterruptDisable.
- **RESET**: Reads vector from 0xFFFC-0xFFFD, clears all registers, sets SP to 0xFF and InterruptDisable flag.

## Future Emulator Direction

This architecture prepares for multiple CPU cores and machine profiles:

- **New CPUs** (e.g. 6502, Z80, 8086) can be implemented as separate classes implementing `ICpuCore`.
- **New machine profiles** (e.g. `C64Profile`, `ZxSpectrumProfile`) implement `IMachineProfile`.
- **New I/O devices** implement `IBusDevice` and optionally `IClockedDevice`.
- The `IMachineBuilder` assembles any combination of CPU + devices.
- Frontends (Blazor, CLI) consume `IMachine` without knowing the internals.
