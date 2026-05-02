# Machine Modules

## Overview

Machine modules are pluggable libraries that provide complete computer system definitions to the emulator frontend. Each module implements the contracts defined in `Symulator.Application.Abstractions` and can be loaded statically (current approach) or dynamically in the future.

## Contracts

### IMachineModule

```csharp
public interface IMachineModule
{
    string Id { get; }                    // e.g. "apple1"
    string DisplayName { get; }           // e.g. "Apple-1"
    string Family { get; }                // e.g. "Apple"
    string Description { get; }
    IReadOnlyList<MachineDescriptor> GetMachines();
    Task<IMachineSession> CreateSessionAsync(string machineId, CancellationToken);
}
```

### IMachineSession

Represents an active emulation session. Each session holds its own machine state and panel ViewModels.

```csharp
public interface IMachineSession : IAsyncDisposable
{
    string MachineId { get; }
    string DisplayName { get; }
    EmulatorStateSnapshot Current { get; }
    IReadOnlyList<IMachinePanelDescriptor> Panels { get; }

    event EventHandler<EmulatorStateSnapshot>? StateChanged;
    event EventHandler<string>? OutputReceived;
    event EventHandler<string>? StatusChanged;

    Task ResetAsync(...);
    Task StepInstructionAsync(...);
    Task RunAsync(...);
    Task PauseAsync(...);
    Task SendInputAsync(...);
    Task ExecuteMachineCommandAsync(string commandId, object? parameter, ...);
}
```

### IMachinePanelDescriptor

Describes a UI panel that the frontend should render for the machine:

```csharp
public interface IMachinePanelDescriptor
{
    string Id { get; }        // e.g. "apple1.terminal"
    string Title { get; }     // e.g. "Terminal"
    string Kind { get; }      // e.g. "apple1.terminal" (for DataTemplate matching)
    object ViewModel { get; } // ViewModel instance the frontend binds to
}
```

## Apple-1 Module

**Project:** `Symulator.Machines.Apple1`

Provides:
- `Apple1MachineModule` — module registration
- `Apple1MachineSession` — session with terminal and boot panels
- `Apple1InputCoordinator` — input queue with mode-aware buffering
- `Apple1TerminalStateMachine` — output tail analysis for prompt detection
- `Apple1PromptDetector` — static prompt detection utilities
- `Apple1TerminalMode` — mode enum (Unknown, Booting, WozMonitor, Basic)
- `Apple1TerminalPanelViewModel` — terminal panel with output/input/mode
- `Apple1BootPanelViewModel` — boot control panel

### Machine Commands

| Command | Effect |
|---------|--------|
| `apple1.boot.monitor` | Start Woz Monitor mode |
| `apple1.boot.basic` | Start BASIC mode |
| `apple1.clear-terminal` | Clear terminal output |
| `apple1.send-line` | Send a line of input |

## KIM-1 Module

**Project:** `Symulator.Machines.Kim1`

Provides:
- `Kim1MachineModule` — module registration
- `Kim1MachineSession` — session with keypad, LED, RIOT panels
- `Kim1KeyMapper` — hex keypad to KIM-1 key mapping
- `Kim1KeypadViewModel` — keypad panel ViewModel
- `Kim1LedDisplayViewModel` — LED display panel ViewModel
- `Kim1RiotStatusViewModel` — RIOT/6530 status panel ViewModel

### Machine Commands

| Command | Effect |
|---------|--------|
| `kim1.reset` | Reset the machine |
| `kim1.clear-display` | Clear LED display |
| `kim1.press-key` | Press a key on the keypad |
| `kim1.release-key` | Release a key |

## Adding a New Machine Module

1. Create a new classlib project `Symulator.Machines.<Name>`
2. Add references to `Symulator.Application` and required `CmosCpu.*` projects
3. Implement `IMachineModule` and `IMachineSession`
4. Create panel ViewModels (implement `INotifyPropertyChanged`)
5. Create Avalonia views in `Symulator.Avalonia.Controls.<Name>`
6. Register DataTemplate in `App.cs`
7. Register module in DI: `collection.AddSingleton<IMachineModule, YourModule>();`
8. Add test project `tests/Symulator.Machines.<Name>.Tests`

No changes to `Symulator.Application` or `Symulator.Avalonia` core are needed.
