# Apple-1 BASIC Interactive Terminal

## Overview

The Apple-1 BASIC Interactive Terminal allows running the Apple-1 computer with Woz Monitor and BASIC ROM in terminal mode, supporting both interactive and script-driven use.

The default boot flow is:
1. Reset CPU → Woz Monitor at `0xFF00`
2. Wait for Woz Monitor to produce output
3. Send `E000R\r` to enter BASIC
4. Wait for BASIC output
5. Enter interactive or script mode

## Requirements

### ROM Files

The Apple-1 profile expects these ROM files:

| File | Address | Description |
|------|---------|-------------|
| `roms/apple-1/Apple-1 ROM.bin` | 0xFF00 | Woz Monitor (256 bytes) |
| `roms/apple-1/Apple-1 BASIC ROM.bin` | 0xE000 | BASIC interpreter (optional) |
| `roms/apple-1/Signetics 2513 Video ROM.bin` | — | Character generator (asset, not CPU-addressable) |

ROMs are loaded automatically when the profile is loaded from file.

## CLI Commands

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic [options]
```

Alias: `apple1` is also registered and maps to the same handler.

### Interactive Terminal (default)

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic --profile profiles/apple-1.json
```

Starts Apple-1 with automatic Woz Monitor → BASIC boot choreography, then enters interactive mode. Keyboard input is sent to the emulated PIA, and terminal output is displayed.

### Woz Monitor only (no BASIC)

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic --profile profiles/apple-1.json --auto-basic false
```

### Direct entry (expert mode, skips Woz Monitor)

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic --profile profiles/apple-1.json --entry 0xE000
```

### Script mode

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic --profile profiles/apple-1.json --script scripts/apple1-basic-smoke.txt --max-cycles 10000000 --trace-boot true
```

## Options

| Option | Default | Description |
|--------|---------|-------------|
| `--profile` | `profiles/apple-1.json` | Apple-1 profile JSON file |
| `--entry` | — | Set entry address (expert mode, skips Woz Monitor) |
| `--script` | — | Script file to feed as keyboard input |
| `--max-cycles` | 10000000 | Maximum CPU cycles |
| `--boot-timeout-cycles` | 1000000 | Boot timeout in cycles (per stage) |
| `--echo-input` | false | Echo typed characters to terminal |
| `--crlf` | apple1 | Line ending mode: `apple1` (LF→CR) or `native` |
| `--auto-basic` | true | Auto-start BASIC via Woz Monitor `E000R` command |
| `--trace-boot` | false | Show boot diagnostics and trace |
| `--exit-on-max-cycles` | (script: true, interactive: false) | Exit when max cycles reached |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Normal exit or script completed with output |
| 1 | Validation error or profile not found |
| 2 | Boot failed or script completed with no output |

## Boot Choreography

When `--auto-basic true` (default):

1. CPU is reset (PC from vector at `0xFFFC`, typically `0xFF00`)
2. CPU executes instructions until Woz Monitor produces PIA output or `--boot-timeout-cycles` is reached
3. Once Woz output is detected, `E000R\r` is sent to the PIA keyboard input
4. CPU continues executing until BASIC output is detected or timeout is reached
5. After boot, the interactive or script loop begins

If boot fails (no Woz output within timeout), the process exits with code 2 and prints diagnostics.

With `--trace-boot true`, boot stages are printed to console.

## Script File Format

Script files are plain text. Each character is queued to the Apple-1 PIA keyboard input as the CPU executes instructions. In `apple1` CR/LF mode, line feed characters (`\n`) are converted to carriage returns (`\r`) for Apple-1 compatibility.

The built-in smoke test script is at `scripts/apple1-basic-smoke.txt`:

```
10 PRINT "HELLO"
20 END
RUN
```

Script mode exits when:
- The CPU halts
- Max cycles are reached (exit code 2 if no output was seen)
- All script characters have been sent and the CPU is idle

## How It Works

1. Profile is loaded (from `--profile` or default `profiles/apple-1.json`)
2. `ComputerMachine` is created with PIA terminal device, ROM files loaded
3. `Apple1BasicBootController` handles boot choreography (Woz → BASIC)
4. After boot, the main loop runs:
   - Polls for keyboard input (interactive) or feeds script characters
   - Queues characters to `Apple1PiaTerminalDevice.QueueKey()`
   - Executes one CPU instruction via `Step()`
   - Checks for new terminal output via cursor-based `ConsumeOutputSince()`
   - Displays output to console
5. Interactive mode never auto-exits on max cycles (unless `--exit-on-max-cycles true`)
6. Script mode exits on max cycles or script completion

## Output Cursor

Output is consumed using `Apple1TerminalOutputCursor` — a record struct that tracks the consumed position independently for each consumer. This prevents output duplication and allows multiple consumers.

```csharp
var cursor = terminal.CreateOutputCursor();
// ... later ...
string output = terminal.ConsumeOutputSince(ref cursor);
```

## Known Limitations

1. **BASIC entry via --entry** — `--entry 0xE000` sets PC directly without Woz Monitor boot choreography; not the default path
2. **No video rendering** — Terminal output is character-based; no video/character-generator emulation
3. **Keyboard input** — Characters are polled from stdin; no interrupt-driven keyboard
4. **Timing** — No real-time throttle; CPU runs as fast as possible
5. **Woz output detection** — Requires Woz Monitor to actually write to PIA output; NOP-only ROMs will not produce detectable output
6. **BASIC ROM required** — Without a valid BASIC ROM at 0xE000, `E000R` will jump to unmapped memory

## Architecture

Key classes:

| Class | Location | Role |
|-------|----------|------|
| `Apple1InteractiveTerminal` | `CmosCpu.Terminal.Tui` | CLI frontend for interactive/script modes |
| `Apple1BasicBootController` | `CmosCpu.Computer` | Boot choreography logic (testable without Console) |
| `Apple1TerminalOutputCursor` | `CmosCpu.Computer` | Immutable output position tracker |
| `Apple1PiaTerminalDevice` | `CmosCpu.Computer` | PIA terminal emulation (keyboard + display) |
