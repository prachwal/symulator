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
| CmosCpu.Computer | Universal machine abstraction: ComputerMachine, IComputerModule, IComputerBus, IComputerCpu, profiles |
| Symulator.Application | Application layer: IEmulatorController, IMachineCatalog, IMachineModule, IMachineSession, EmulatorSnapshot models |
| Symulator.Machines.Apple1 | Apple-1 machine module: terminal, PIA adapter, input coordinator, boot workflow, profiles |
| Symulator.Machines.Kim1 | KIM-1 machine module: keypad, LED display, RIOT/6530 status, profiles |
| Symulator.Avalonia | Avalonia UI frontend: views, ViewModels, controls, DI wiring |

## Dependencies

```
CmosCpu.Core (no deps)
CmosCpu.Bus -> CmosCpu.Core
CmosCpu.Memory -> CmosCpu.Core, CmosCpu.Bus
CmosCpu.Cpu -> CmosCpu.Core, CmosCpu.Bus
CmosCpu.Devices -> CmosCpu.Core, CmosCpu.Bus
CmosCpu.Assembler -> CmosCpu.Core
CmosCpu.Runtime -> CmosCpu.Core, CmosCpu.Bus, CmosCpu.Memory, CmosCpu.Cpu, CmosCpu.Devices, CmosCpu.Assembler
CmosCpu.Computer -> CmosCpu.Core, CmosCpu.Bus, CmosCpu.Memory, CmosCpu.Cpu, CmosCpu.Devices, CmosCpu.Runtime

Symulator.Application -> CmosCpu.Core, CmosCpu.Cpu, CmosCpu.Bus, CmosCpu.Memory, CmosCpu.Devices, CmosCpu.Assembler, CmosCpu.Runtime, CmosCpu.Computer

Symulator.Machines.Apple1 -> CmosCpu.Core, CmosCpu.Computer, CmosCpu.Runtime, CmosCpu.Devices, Symulator.Application
Symulator.Machines.Kim1   -> CmosCpu.Core, CmosCpu.Computer, CmosCpu.Runtime, CmosCpu.Devices, Symulator.Application

Symulator.Avalonia -> Symulator.Application, Symulator.Machines.Apple1, Symulator.Machines.Kim1
```

## Machine Module Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Symulator.Avalonia                    │
│  ┌──────────┐ ┌──────────┐ ┌─────────────────────────┐ │
│  │ Apple-1  │ │  KIM-1   │ │ Common: CPU State, ...  │ │
│  │ Views    │ │  Views   │ │                         │ │
│  └────┬─────┘ └────┬─────┘ └─────────────────────────┘ │
│       │            │           ▲                        │
└───────┼────────────┼───────────┼────────────────────────┘
        │            │           │
┌───────┴────────────┴───────────┴────────────────────────┐
│               Symulator.Application                     │
│  ┌────────────────────────────────────────────────────┐ │
│  │ IEmulatorController, IMachineCatalog, IMachineModule│ │
│  │ IMachineSession, EmulatorSnapshot, MachineDescriptor│ │
│  └────────────────────────────────────────────────────┘ │
└───────┬────────────┬────────────────────────────────────┘
        │            │
┌───────┴──────┐ ┌──┴───────────┐
│Apple1Module │ │ Kim1Module   │
│  IMachineModule  │             │
│  IMachineSession │             │
│  Panel VMs       │             │
└────────────────┘ └─────────────┘
```

## UI Composition

The Avalonia frontend uses DataTemplates to map machine-specific ViewModels to visual controls:

- `Apple1TerminalPanelViewModel` → `Apple1TerminalPanelView`
- `Apple1BootPanelViewModel` → `Apple1BootPanelView`
- `Kim1KeypadViewModel` → `Kim1KeypadView`
- `Kim1LedDisplayViewModel` → `Kim1LedDisplayView`
- `Kim1RiotStatusViewModel` → `Kim1RiotStatusView`

The `MainWindow` contains a `ContentControl` bound to `ActivePanelViewModel`. When a machine is selected, the first panel from the session's `IMachinePanelDescriptor` list is displayed, and Avalonia resolves the correct view via DataTemplates.

No machine-specific logic exists in the main ViewModel or in Symulator.Application.

### Machine Commands

Sessions accept machine-specific commands via `ExecuteMachineCommandAsync`:

- Apple-1: `apple1.boot.monitor`, `apple1.boot.basic`, `apple1.clear-terminal`, `apple1.send-line`
- KIM-1: `kim1.reset`, `kim1.clear-display`, `kim1.press-key`, `kim1.release-key`

## DI Configuration

Services are registered in `App.cs` using `Microsoft.Extensions.DependencyInjection`:

```csharp
collection.AddSingleton<IMachineModule, Apple1MachineModule>();
collection.AddSingleton<IMachineModule, Kim1MachineModule>();
collection.AddSingleton<MachineCatalog>();
collection.AddSingleton<IMachineCatalog>(sp => sp.GetRequiredService<MachineCatalog>());
collection.AddSingleton<IEmulatorController, EmulatorController>();
collection.AddTransient<MainWindowViewModel>();
```

## Legacy

The following frontends have been removed:
- **CmosCpu.ConsoleApp** — CLI application (removed in favor of Avalonia)
- **CmosCpu.BlazorApp** — Blazor Server diagnostic frontend (removed in favor of Avalonia)
- **CmosCpu.Terminal** — Terminal.Gui TUI (removed in favor of Avalonia)
