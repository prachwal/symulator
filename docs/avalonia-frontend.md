# Avalonia Frontend

## Overview

`Symulator.Avalonia` is the sole graphical frontend for the CmosCpu emulator framework. It is a cross-platform desktop application built with [Avalonia UI](https://www.avaloniaui.net/) 11.2.

## Running

```bash
dotnet run --project src/Symulator.Avalonia
```

## Architecture

The frontend follows the MVVM pattern with ViewModels in `Symulator.Avalonia.ViewModels` and views (`.axaml` + code-behind) in `Symulator.Avalonia.Views` and `Symulator.Avalonia.Controls.*`.

### Main Window Layout

```
+--------------------------------------------------------------------------------+
| Menu / Toolbar: Start, Pause, Reset, Step                                       |
+----------------------+--------------------------------------+------------------+
| Machine List         | Main Machine Surface                 | Inspector        |
| - Apple-1            | (ContentControl bound to             | - CPU registers  |
| - KIM-1              |  ActivePanelViewModel)               | - Flags          |
|                      |                                      | - Cycle count    |
+----------------------+--------------------------------------+------------------+
| Status bar: selected machine, running state, last error                         |
+--------------------------------------------------------------------------------+
```

### DI Wiring

Dependency injection is configured in `App.cs` using `Microsoft.Extensions.DependencyInjection`:

- `IMachineModule` instances are registered as singletons (Apple-1, KIM-1)
- `MachineCatalog` aggregates all modules
- `IEmulatorController` manages session lifecycle
- `MainWindowViewModel` is transient and receives the controller via constructor injection

### Machine Panel Hosting

The `MainWindow` contains a `ContentControl` bound to `ActivePanelViewModel`. When the user selects a machine:

1. `MainWindowViewModel.OnMachineSelected` calls `IEmulatorController.SelectMachineAsync`
2. The controller creates an `IMachineSession` via the matching module
3. The first panel descriptor's `ViewModel` is assigned to `ActivePanelViewModel`
4. Avalonia DataTemplates resolve the correct view control for the ViewModel type

### DataTemplates

All machine-specific DataTemplates are registered in `App.cs.Initialize()`:

```csharp
DataTemplates.Add(new FuncDataTemplate<Apple1TerminalPanelViewModel>(
    (vm, _) => new Apple1TerminalPanelView { DataContext = vm }));
```

## Project References

- `Symulator.Application` — contracts, controller, catalog
- `Symulator.Machines.Apple1` — Apple-1 ViewModels
- `Symulator.Machines.Kim1` — KIM-1 ViewModels

No direct references to CmosCpu.* core projects exist in the Avalonia project; all access goes through `Symulator.Application` and machine modules.

## Controls

### Apple-1
- `Controls/Apple1/Apple1TerminalPanelView` — terminal output, input field, boot buttons
- `Controls/Apple1/Apple1BootPanelView` — boot options (MON, BASIC)

### KIM-1
- `Controls/Kim1/Kim1ControlPanelView` — combined panel with LED display
- `Controls/Kim1/Kim1LedDisplayView` — 6-digit LED display
- `Controls/Kim1/Kim1KeypadView` — hex keypad with Enter/Backspace
- `Controls/Kim1/Kim1RiotStatusView` — RIOT/6530 port and DDR status

## Testing

The `Symulator.Avalonia.Tests` project (not yet created) will test ViewModel interactions with mocked `IEmulatorController` instances.
