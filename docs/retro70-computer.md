# Retro70 MOS 6502 Computer

## Overview

Retro70 is a simple educational computer based on the MOS 6502 CPU, inspired by classic late-1970s machines like the KIM-1, Apple I, and Commodore PET. It is designed as a learning platform for 6502 assembly programming, not as a Commodore 64 emulator.

## Architecture

| Component | Specification |
|-----------|---------------|
| CPU | MOS 6502 @ 1 MHz (all 56 official opcodes) |
| RAM | 32 KB (0x0000 — 0x7FFF) |
| ROM | 4 KB Monitor (0xF000 — 0xFFFF) |
| Text Display | 40×25 character buffer at 0xD000 |
| Keyboard | Memory-mapped at 0xD800 (data) / 0xD801 (status) |

## Memory Map

```
0x0000 — 0x7FFF  RAM (32 KB)
0x8000 — 0xCFFF  Available / expansion
0xD000 — 0xD3E7  Text Display (40×25 = 1000 bytes)
0xD800           Keyboard data port
0xD801           Keyboard status port (1 = key waiting)
0xF000 — 0xFFFF  Monitor ROM (4 KB)
  0xFFFA — 0xFFFB  NMI vector
  0xFFFC — 0xFFFD  RESET vector
  0xFFFE — 0xFFFF  IRQ vector
```

## JSON Profile

The computer is described by a JSON profile. The default profile is at `profiles/retro70-mos6502.json`:

```json
{
  "id": "retro70-mos6502",
  "name": "Retro70 MOS 6502 Computer",
  "cpu": "mos6502",
  "clockHz": 1000000,
  "memory": {
    "ram": [
      { "start": "0x0000", "size": "0x8000" }
    ],
    "rom": [
      { "start": "0xF000", "size": "0x1000", "file": "roms/retro70-monitor.bin" }
    ],
    "vectors": {
      "reset": "0xF000",
      "nmi": "0xF100",
      "irq": "0xF200"
    }
  },
  "devices": [
    {
      "type": "text-display",
      "id": "screen",
      "start": "0xD000",
      "width": 40,
      "height": 25
    },
    {
      "type": "keyboard",
      "id": "keyboard",
      "dataAddress": "0xD800",
      "statusAddress": "0xD801"
    }
  ]
}
```

## Monitor ROM

The built-in monitor ROM at 0xF000 displays:

```
RETRO70 READY
>
```

Then enters an infinite loop. After reset, the CPU reads the reset vector at 0xFFFC and begins execution at 0xF000.

## How to Run (Blazor UI)

1. Start the Blazor app: `dotnet run --project src/CmosCpu.BlazorApp`
2. Navigate to `http://localhost:5113/retro70`
3. The page automatically loads the Retro70 profile and monitor ROM
4. You see:
   - **Text Screen**: 40×25 character display showing "RETRO70 READY>"
   - **CPU State**: A, X, Y, PC, SP, Cycles, and flags (N, V, B, D, I, Z, C)
   - **Memory Map**: Table of all mapped regions
5. Use toolbar buttons: Reset, Step, Run 100, Run 1000, Stop, Clear screen

## Loading a Binary Program

1. Enter the start address (e.g., `0x8000`)
2. Click "Choose .bin file" and select a 6502 binary
3. Click "Load Binary" — the binary is written to RAM and the reset vector is updated

## Example ASM Program

To write 6502 assembly:

```asm
; Blink pattern — write to screen
    LDA #$FF       ; A9 FF
    STA $D000      ; 8D 00 D0 — write to first screen position
    JMP $F000      ; 4C 00 F0 — return to monitor (example)
```

Assemble manually or use the Simulator page's ASM editor for the educational CPU (note: the educational CPU has a **different** instruction set from the MOS 6502).

## Keyboard

- Type in the text field to send keystrokes to the 6502
- Key data appears at 0xD800, status at 0xD801
- The monitor ROM does not implement keyboard reading — it's available for custom programs

## Project Structure

| Project | Purpose |
|---------|---------|
| `CmosCpu.Core` | Shared interfaces (`IMemoryBus`), profile types (`ComputerProfile`, `ComputerProfileLoader`) |
| `CmosCpu.Cpu` | `Mos6502Cpu` — full 6502 emulation |
| `CmosCpu.Computer` | `ComputerMachine`, `ComputerMemoryBus`, `TextDisplayRegion`, `KeyboardRegion`, `Retro70MonitorRomBuilder`, `Retro70Service` |
| `CmosCpu.BlazorApp` | Web UI — Retro70 page at `/retro70` |

## Limitations

- **Not cycle-accurate**: The CPU counts instruction cycles but does not model individual bus read/write cycles
- **No undocumented opcodes**: Only the 56 official MOS 6502 opcodes are implemented
- **Not a C64**: No VIC-II, CIA, SID, or 6510 I/O port
- **No real-time clock**: Execution is step-based (RunSteps), not timed
- **Simple text display**: 1 byte = 1 ASCII character, no cursor, no scrolling
- **No assembler**: Assembly must be done externally using a 6502 assembler of your choice

## Tests

Tests are in `tests/CmosCpu.Computer.Tests/`:

- `ComputerMachineTests.cs` — memory bus, machine creation, basic execution
- `Retro70MonitorRomTests.cs` — ROM size, vectors, screen output after execution
