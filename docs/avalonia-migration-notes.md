# Avalonia Migration Notes

## Solution Map (commit cfcf4a2+)

### Projects

| Project | Type | Notes |
|---------|------|-------|
| `CmosCpu.Core` | Domain | Core abstractions (IMemoryBus, etc.) |
| `CmosCpu.Bus` | Domain | Memory bus implementation |
| `CmosCpu.Cpu` | Domain | MOS 6502 CPU |
| `CmosCpu.Memory` | Domain | RAM/ROM mapping |
| `CmosCpu.Devices` | Domain | I/O devices (PIA, display, keypad) |
| `CmosCpu.Assembler` | Domain | Simple 6502 assembler |
| `CmosCpu.Runtime` | Domain | Simulator runtime, loader |
| `CmosCpu.Computer` | Domain | Machine profiles, device wiring, ComputerMachine |
| **`CmosCpu.ConsoleApp`** | **UI** | **To remove** — 117-line Program.cs, no domain logic |
| **`CmosCpu.BlazorApp`** | **UI** | **To remove** — Blazor Server UI, services, models, Razor components |
| **`CmosCpu.Terminal`** | **Mixed** | **To refactor** — contains TUI views AND shared domain logic |
| `CmosCpu.Assembler.Tests` | Test | Keep |
| `CmosCpu.BlazorApp.Tests` | Test | To remove/replace |
| `CmosCpu.Bus.Tests` | Test | Keep |
| `CmosCpu.Computer.Tests` | Test | Keep |
| `CmosCpu.Core.Tests` | Test | Keep |
| `CmosCpu.Cpu.Tests` | Test | Keep |
| `CmosCpu.Devices.Tests` | Test | Keep |
| `CmosCpu.Memory.Tests` | Test | Keep |
| `CmosCpu.Runtime.Tests` | Test | Keep |
| `CmosCpu.Terminal.Tests` | Test | To refactor — merge tests for preserved classes |

### What each UI project contains

#### CmosCpu.ConsoleApp
- Single `Program.cs` (117 lines) — parses `--program`, `--cycles`, `--trace`, runs simulator
- No reusable domain logic — **safe to delete immediately**

#### CmosCpu.BlazorApp
- **To preserve:** `AddressParser.cs` (utility, used by terminal too)
- **To preserve:** `IBlazorSimulationController.cs` abstraction
- **To discard:** `BlazorSimulationController.cs` (Blazor-specific implementation)
- **To discard:** `EmulatorUiSettings.cs`, `IEmulatorUiSettings.cs`
- **To discard:** `ProgramUploadService.cs`, `IProgramUploadService.cs`
- **To discard:** All `Models/*.cs` (view models for Blazor)
- **To discard:** All `Infrastructure/*.cs` (Blazor DI config)
- **To discard:** All `Components/` (Razor views)
- **To discard:** `Program.cs`, `App.razor`, `_Imports.razor`, `wwwroot/`

#### CmosCpu.Terminal
- **To preserve (move to Application/Abstractions):**
  - `TerminalTextRingBuffer` — common bounded text buffer
  - `Apple1InputCoordinator` — Apple-1 input coordination
  - `Apple1TerminalStateMachine` — prompt detection state machine
  - `Apple1TerminalMode` — mode enum
  - `Apple1PromptDetector` — prompt detection (used by CLI mode)
  - `Kim1KeyMapper` — PC→KIM-1 key mapping
  - `ITerminalScreen` — screen abstraction
  - `ITerminalScreenFactory` — factory interface
  - `TerminalScreenFactory` — factory impl
  - `TerminalPlatformDescriptor` — platform descriptor
  - `TerminalProfileLoader` — profile loader
  - `TerminalRunLoopOptions` — run loop configuration
  - `TerminalCpuPanel` — CPU panel view (Terminal.Gui bound)
  - `TerminalHelpBar` — help bar (Terminal.Gui bound)
  - `TerminalGuiColorScheme` — color scheme (Terminal.Gui bound)
  - `TerminalGuiRenderer` — render helpers (Terminal.Gui bound)
- **To discard (TUI-specific, Terminal.Gui):**
  - `Apple1TuiScreen` — Apple-1 Terminal.Gui screen
  - `Kim1TuiScreen` — KIM-1 Terminal.Gui screen
  - `TerminalApp` — command router
  - `Apple1InteractiveTerminal` — CLI interactive terminal
  - `InteractiveShell` — CLI shell
  - `BatchCommands` — CLI batch commands
  - `CommandRouter` — CLI command dispatch
  - `Session/ComputerSession`, `ISimulatorSession` — session management

### Strategy

1. Delete `CmosCpu.ConsoleApp` — no domain logic to preserve.
2. Delete `CmosCpu.BlazorApp` — preserve `AddressParser.cs` only.
3. Refactor `CmosCpu.Terminal`:
   - Extract non-UI classes into a new `Symulator.Application` project (or keep in Terminal but without Terminal.Gui dependency).
   - Delete Terminal.Gui dependent classes.
   - Delete `Session/`, `Commands/`, `Tui/` leaves-only folders.
4. Rename/migrate `CmosCpu.Terminal` to `Symulator.Application` or keep as Terminal with only logic.
5. Create `Symulator.Avalonia` as new desktop frontend.
6. Test projects: delete `CmosCpu.BlazorApp.Tests`, refactor `CmosCpu.Terminal.Tests` to test preserved logic.

### Risk assessment

- `ISimulatorSession` is used by `TerminalApp`, `BatchCommands`, and screen classes — refactoring will touch many files.
- `ComputerSession` has profile loading logic needed by Avalonia.
- `Terminal.Gui` dependency in Terminal project blocks reuse in Avalonia.
- `CmosCpu.Terminal.Tests` has 117 tests — many test logic that should survive.
- The solution file (`.slnx`) must be updated.

### Next steps

1. Delete ConsoleApp.
2. Delete BlazorApp (except AddressParser).
3. Extract Terminal pure-logic classes → keep, delete TG-dependent classes.
4. Build + fix.
5. Add Avalonia project.
