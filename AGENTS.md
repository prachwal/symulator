# Symulator — Agent Guide

## Approach
Make one focused change at a time. Prefer fixing existing code over new abstractions.

## Project
.NET 10 C# solution (`CmosCpuSimulator.slnx`) — 8-bit CPU simulator with Avalonia UI.
12 source projects, 11 test projects. Clean architecture: Core → Bus/Memory/Cpu/Devices/Assembler → Runtime → Computer → Application → Machine modules → Avalonia.

Machine-specific code stays in `src/Symulator.Machines.Apple1` or `src/Symulator.Machines.Kim1`. Generic UI shell stays in `src/Symulator.Avalonia`. Generic orchestration stays in `src/Symulator.Application`. Do not move machine-specific code into `Symulator.Avalonia`.

## Build & Test
```bash
dotnet build CmosCpuSimulator.slnx     # builds all 23 projects
dotnet build                           # also works (same result)
```

After a successful build, use `--no-build` for test re-runs. Do not run full `dotnet test` — it will hang. Run per-project instead:

```bash
dotnet test tests/Symulator.Application.Tests --no-build
dotnet test tests/Symulator.Machines.Apple1.Tests --no-build
dotnet test tests/Symulator.Machines.Kim1.Tests --no-build
dotnet test tests/CmosCpu.Cpu.Tests --no-build --filter "FullyQualifiedName!~Reference"
dotnet test tests/CmosCpu.Computer.Tests --no-build --filter "FullyQualifiedName!~RunController"
```

## Known Test Issues
- **`dotnet test` (any/all) will hang** — `ComputerRunControllerTests` have infinite loops. Run per-project.
- **5 failing tests** in `CmosCpu.Computer.Tests` (keyboard, output stream, Apple-1 BASIC CR/LF) — do not fix unless asked.
- **Reference CPU tests** need Klaus Dormann 6502 ROMs placed manually in `tests/CmosCpu.Cpu.Tests/Reference/roms/`. Report `Inconclusive` when absent.

## Framework & Conventions
- **Test framework:** MSTest 4 + Moq 4 + FluentAssertions 8 (not xUnit, not NUnit).
- **Nullability:** `<Nullable>enable</Nullable>` — all projects use nullable reference types.
- **DI:** `Microsoft.Extensions.DependencyInjection` 10 — configured in `Symulator.Avalonia/App.cs`.
- **Logging:** NLog 6 via `NLog.config` (copied to output).
- **Solution format:** `.slnx` (new XML format).
- **No `any` / no `dynamic` / no broad `object` casts** — use proper interfaces.
- **No blocking waits** (`.Result`, `.Wait()`) — use async/await.
- **No fire-and-forget tasks** without error handling.
- **Avalonia threading:** Never update UI-bound VMs from worker threads. Marshal with `Dispatcher.UIThread.Post(...)` inside `Symulator.Avalonia` only. Do not reference Avalonia from `Symulator.Application`.

## Logging
Use NLog. Log machine selection, profile/ROM paths, reset vector, PC after reset, boot success/failure with diagnostics, exceptions with stack trace. Do not log every emulator snapshot at Debug — use Trace for high-frequency state updates.

## Entry Points
| Layer | Key Type | File |
|-------|----------|------|
| UI | `Program.Main()` → `Symulator.Avalonia` | `src/Symulator.Avalonia/Program.cs` |
| DI/App | `App` | `src/Symulator.Avalonia/App.cs` |
| Main VM | `MainWindowViewModel` | `src/Symulator.Avalonia/ViewModels/MainWindowViewModel.cs` |
| Controller | `EmulatorController` | `src/Symulator.Application/Services/EmulatorController.cs` |
| Machine abstraction | `IMachineSession` | `src/Symulator.Application/Abstractions/IMachineSession.cs` |
| Machine runtime | `ComputerMachine` | `src/CmosCpu.Computer/ComputerMachine.cs` |
| CPU core | `Mos6502Cpu` | `src/CmosCpu.Cpu/Mos6502Cpu.cs` |
| Machine profiles | `profiles/apple-1.json`, `profiles/kim-1.json` | `src/Symulator.Avalonia/` |

## Machine Modules
| Module | Session | Workspace VM | Factory |
|--------|---------|-------------|---------|
| Apple-1 (`"apple1"`) | `Apple1MachineSession` | `Apple1WorkspaceViewModel` | `Apple1MachineFactory` |
| KIM-1 (`"kim1"`) | `Kim1MachineSession` | `Kim1WorkspaceViewModel` | `Kim1MachineFactory` |

Each machine DLL owns its complete implementation (module, factory, session, workspace VM, workspace view, boot logic, ROM/profile diagnostics). The Avalonia shell only hosts the selected workspace.

## Forbidden Actions
- Restore console/TUI/Blazor projects.
- Move machine-specific UI into `Symulator.Avalonia`.
- Run full unfiltered `dotnet test`.
- Ignore/swallow exceptions with empty `catch`.
- Add fake terminal output just to satisfy the UI.
- Hardcode absolute paths.
- Modify CPU core unless evidence points there.
