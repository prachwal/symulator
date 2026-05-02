# Avalonia Migration Status

## Current state (after migration)

### Existing projects (18 total)

#### Source (10)
| Project | Status |
|---------|--------|
| CmosCpu.Core | Keep |
| CmosCpu.Bus | Keep |
| CmosCpu.Cpu | Keep |
| CmosCpu.Memory | Keep |
| CmosCpu.Devices | Keep |
| CmosCpu.Assembler | Keep |
| CmosCpu.Runtime | Keep |
| CmosCpu.Computer | Keep |
| **Symulator.Application** | **New — abstractions + moved logic** |
| **Symulator.Avalonia** | **In progress — skeleton** |

#### Test (8)
Core, Bus, Cpu, Memory, Devices, Assembler, Computer, Runtime tests — all kept.

### Removed
- CmosCpu.ConsoleApp
- CmosCpu.BlazorApp + BlazorApp.Tests
- **CmosCpu.Terminal** + Terminal.Tests (Terminal.Gui TUI)
- docs/blazor-performance-notes.md

### Symulator.Application contents

**Abstractions:**
- `IEmulatorController` — run/pause/reset/step/select machine/send input
- `IMachineCatalog` — list available machine profiles

**Models:**
- `EmulatorStateSnapshot`, `CpuStateSnapshot`, `CpuRegisterSnapshot`
- `MachineDescriptor`
- `Apple1TerminalMode` (moved from Terminal)
- `TerminalTextRingBuffer` (moved from Terminal)

**Services:**
- `Apple1InputCoordinator` (moved)
- `Apple1TerminalStateMachine` (moved)
- `Apple1PromptDetector` (moved)
- `Kim1KeyMapper` (moved)
- `AddressParser` (moved)

### Remaining work
1. Implement `IEmulatorController` in Application
2. Connect Avalonia ViewModels to controller
3. Add Application tests
4. Update README + docs
5. Final build + test
