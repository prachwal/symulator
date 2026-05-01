# Blazor Frontend

## Architecture

The Blazor frontend (`CmosCpu.BlazorApp`) is a web-based GUI for the CmosCpu Simulator. It uses Blazor Interactive Server mode — all simulation logic runs server-side, and the UI updates are pushed to the browser via SignalR.

```
Browser          SignalR          Server (Blazor Interactive Server)
┌────────┐                      ┌──────────────────────────────┐
│  HTML  │ ◄────────────────►  │  Blazor Components (.razor)  │
│  CSS   │     WebSocket       │  Services / Controller        │
│  JS    │                      │  CmosCpu.Runtime              │
└────────┘                      │  CmosCpu.Core / Bus / CPU    │
                                └──────────────────────────────┘
```

## Differences from WPF

| Aspect | WPF | Blazor |
|--------|-----|--------|
| UI Technology | XAML + native Windows | HTML/CSS + SignalR |
| Platform | Windows only | Cross-platform (browser) |
| State Binding | INotifyPropertyChanged | StateHasChanged + manual re-render |
| Commands | ICommand | EventCallback / onclick handlers |
| Layout | Grid / StackPanel div | CSS flexbox / grid |
| File Upload | OpenFileDialog | InputFile component |
| Circuit | N/A | SignalR circuit (subject to reconnection) |

## Integration with Runtime

The Blazor app uses the same `CmosCpu.Runtime.Simulator` as the ConsoleApp. Communication flows through `IBlazorSimulationController`:

```
Razor Component → IBlazorSimulationController
    ↓                          ↓
StateHasChanged()        BlazorSimulationController
    ↓                          ↓
UI re-renders             CmosCpu.Runtime.Simulator
```

### Event flow

1. `Step()` executes one CPU instruction.
2. The Simulator fires `TraceExecuted` and `SnapshotChanged` events.
3. `BlazorSimulationController` relays these as `TraceEntryAdded` and `SnapshotChanged`.
4. The `Simulator.razor` page subscribes to these events and calls `InvokeAsync(StateHasChanged)`.
5. All child components re-render with fresh data from `UiSimulatorState`.

## File Upload

- `.asm` files are read as text, assembled by `SimpleAssembler`, loaded into ROM.
- `.bin` files are read as raw bytes, loaded into ROM at the specified address.
- Maximum file size: 1 MB.
- Validation: extension check (.asm / .bin) + size check.
- Errors are shown in the status bar via `StatusChanged` event.

## State Update

The `UiSimulatorState` model aggregates all display data from `SimulatorSnapshot`:

- CPU registers and flags
- LED state
- Timer and RTC values
- Interrupt vectors
- Memory rows (16-column hex + ASCII)
- Trace entries and bus transactions

Updates happen on `SnapshotChanged` event, which is fired after each `Step()` call and at the end of `Run()`.

## Limitations of Interactive Server Mode

- Requires persistent SignalR connection. If the connection drops, the circuit is lost and the UI resets.
- All CPU cycles run on the server. Long-running simulations without pause may consume server CPU.
- Blazor Server has a circuit timeout default of 30 seconds of inactivity.
- Not suitable for multi-user scenarios (each user gets their own simulator instance).

## Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run Blazor app
dotnet run --project src/CmosCpu.BlazorApp

# Open browser at:
# https://localhost:5001/simulator
```

## Pages

| Route | Description |
|-------|-------------|
| `/simulator` | Main simulator panel with toolbar, registers, memory, logs |
| `/instruction-set` | Complete instruction set table |
| `/memory-map` | Memory map, I/O devices, system vectors |

## Components

| Component | File | Description |
|-----------|------|-------------|
| Simulator | `Pages/Simulator.razor` | Main page orchestrating all panels |
| Toolbar | `Simulator/Toolbar.razor` | Load, Reset, Step, Run, Pause, Stop, Speed |
| CpuRegistersPanel | `Simulator/CpuRegistersPanel.razor` | A, X, Y, PC, SP, Cycles |
| FlagsPanel | `Simulator/FlagsPanel.razor` | Zero, Carry, Negative, I.Disable badges |
| LedPanel | `Simulator/LedPanel.razor` | ON/OFF with LED indicator |
| MemoryViewer | `Simulator/MemoryViewer.razor` | 16-column hex + ASCII table |
| TraceLog | `Simulator/TraceLog.razor` | Instruction execution log |
| BusLog | `Simulator/BusLog.razor` | Bus transaction log |
| DevicesPanel | `Simulator/DevicesPanel.razor` | Timer + RTC |
| TimerPanel | `Simulator/TimerPanel.razor` | Timer registers |
| RtcPanel | `Simulator/RtcPanel.razor` | RTC date/time |
| InterruptsPanel | `Simulator/InterruptsPanel.razor` | Interrupt state |
