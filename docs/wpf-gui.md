# WPF GUI

## Architecture

The WPF GUI follows the **MVVM (Model-View-ViewModel)** pattern:

```
┌──────────┐     ┌──────────────┐     ┌──────────┐
│  Views   │ ←── │  ViewModels  │ ←── │  Models  │
│ (.xaml)  │     │  (.cs)       │     │  (Core)  │
└──────────┘     └──────┬───────┘     └──────────┘
                        │
                  ┌─────▼──────┐
                  │  Services  │
                  │  (Runtime) │
                  └────────────┘
```

### Layers

| Layer | Description |
|-------|-------------|
| **Views** | XAML UserControls with data binding to ViewModels. No code-behind logic. |
| **ViewModels** | INotifyPropertyChanged classes exposing observable properties and ICommand bindings. |
| **Services** | IUiSimulationController bridges Runtime and UI; IDispatcherService handles thread marshaling; IFileDialogService abstracts file picker. |
| **Models** | SimulatorSnapshot and related types from CmosCpu.Core. |

## Integration with Runtime

The GUI communicates with the simulator exclusively through:

- `IUiSimulationController` — high-level operations (Load, Reset, Step, Run, Pause, Stop)
- `SimulatorSnapshot` — atomic state snapshots
- Event handlers for trace entries and bus transactions

```
WPF ViewModel
    │
    ▼
IUiSimulationController
    │
    ├── Simulator.LoadProgram() / Reset() / Step()
    ├── event SnapshotChanged → UI update via Dispatcher
    └── event TraceEntryAdded → TraceLog update via Dispatcher
```

## Simulation Loop

The `RunAsync` method runs on a background thread:

```csharp
public async Task RunAsync(CancellationToken cancellationToken)
{
    while (!token.IsCancellationRequested && !_simulator.Cpu.Halted)
    {
        _simulator.Step();
        if (_speedHz > 0 && _speedHz < 10000)
            await Task.Delay(1000 / _speedHz, token);
        await Task.Yield();
    }
}
```

- The loop never blocks the UI thread.
- The dispatcher marshals events back to the UI thread.
- Speed is controlled by `SpeedHz` (1, 10, 100, 1000, or Max = no delay).
- `Pause()` sets a flag that stops the loop without resetting CPU state.
- `Stop()` stops the loop and resets the CPU.

## UI Update Flow

1. `Simulator.Step()` executes one instruction.
2. `Simulator.TraceExecuted` event fires with a `TraceEntry`.
3. `Simulator.SnapshotChanged` event fires with a `SimulatorSnapshot`.
4. `UiSimulationController` receives events and dispatches them to the UI thread.
5. ViewModels update their observable properties.
6. WPF data binding automatically refreshes the XAML views.

## Debugging Programs

1. **Load** an `.asm` file via the toolbar.
2. Use **Step** to execute one instruction at a time.
3. Watch **Registers** and **Flags** update after each step.
4. The **Memory Viewer** shows the memory around the current PC, highlighted in blue.
5. The **Trace Log** records every executed instruction with cycle count, PC, opcode, and register state.
6. Use **Run** for continuous execution, **Pause** to inspect state mid-execution.

## Speed Control

| Setting | Actual Speed |
|---------|-------------|
| 1 Hz | 1 instruction per second |
| 10 Hz | 10 instructions per second |
| 100 Hz | 100 instructions per second |
| 1 kHz | 1000 instructions per second |
| Max | No delay (as fast as possible) |

## Limitations on Linux

WPF is a Windows-only presentation framework. This application:

- **Compiles** on Linux with `EnableWindowsTargeting=true` (the .NET 10 SDK includes WPF reference assemblies).
- **Does not run** on Linux — no WPF runtime is available.
- Must be built and executed on **Windows** with the .NET 10 SDK.

To build on Windows:

```powershell
dotnet build src/CmosCpu.WpfApp
dotnet run --project src/CmosCpu.WpfApp
```

## File Structure

```
src/CmosCpu.WpfApp/
├─ App.xaml / App.xaml.cs          — Application entry point, DI setup
├─ MainWindow.xaml / .cs           — Main layout window
├─ ViewModels/
│  ├─ ViewModelBase.cs             — INotifyPropertyChanged base
│  ├─ MainWindowViewModel.cs       — Toolbar commands, sub-VM orchestration
│  ├─ RegistersViewModel.cs        — CPU register display
│  ├─ FlagsViewModel.cs            — CPU flags display
│  ├─ MemoryViewModel.cs           — Memory hex viewer
│  ├─ LedViewModel.cs              — LED state indicator
│  ├─ TraceLogViewModel.cs         — Instruction trace log
│  ├─ DevicesViewModel.cs          — Timer/RTC device state
│  └─ InterruptsViewModel.cs       — Interrupt/vector state
├─ Services/
│  ├─ IUiSimulationController.cs   — Controller interface
│  ├─ UiSimulationController.cs    — Controller implementation
│  ├─ IFileDialogService.cs        — File dialog abstraction
│  ├─ FileDialogService.cs         — Win32 file dialog
│  ├─ IDispatcherService.cs        — UI thread dispatch
│  └─ DispatcherService.cs         — WPF Dispatcher wrapper
├─ Commands/
│  ├─ RelayCommand.cs              — Synchronous ICommand
│  └─ AsyncRelayCommand.cs         — Async ICommand with cancellation
├─ Converters/
│  ├─ BoolToOnOffConverter.cs      — bool → "ON"/"OFF"
│  ├─ ByteToHexConverter.cs        — byte → "0x{XX}"
│  ├─ UShortToHexConverter.cs      — ushort → "0x{XXXX}"
│  └─ FlagToBrushConverter.cs      — bool → green/dark brush
├─ Views/
│  └─ CpuStateView.xaml / .cs      — Placeholder view
└─ Resources/
   ├─ Colors.xaml                  — Dark theme colors
   └─ Styles.xaml                  — Dark theme styles
```
