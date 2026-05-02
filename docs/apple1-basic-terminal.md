# Apple-1 BASIC Interactive Terminal

## Overview

The Apple-1 BASIC Interactive Terminal allows running the Apple-1 computer with Woz Monitor and BASIC ROM in terminal mode, supporting both interactive and script-driven use.

## Requirements

### ROM Files

The Apple-1 profile expects these ROM files:

| File | Address | Description |
|------|---------|-------------|
| `roms/apple-1/Apple-1 ROM.bin` | 0xFF00 | Woz Monitor (256 bytes) |
| `roms/apple-1/Apple-1 BASIC ROM.bin` | 0xE000 | BASIC interpreter (optional) |
| `roms/apple-1/Signetics 2513 Video ROM.bin` | — | Character generator (asset, not CPU-addressable) |

ROMs are loaded automatically when the profile is loaded.

## CLI Commands

### Interactive Terminal

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic --profile profiles/apple-1.json
```

Starts the Apple-1 in interactive mode. The CPU runs step-by-step, processing keyboard input and displaying terminal output.

### Script Mode

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic --profile profiles/apple-1.json --script scripts/apple1-basic-smoke.txt --max-cycles 2000000
```

Reads a script file and feeds its contents as keyboard input to the Apple-1. The script's `\n` characters are converted to `\r` for Apple-1 compatibility in the default CR/LF mode.

### Options

| Option | Default | Description |
|--------|---------|-------------|
| `--profile` | `profiles/apple-1.json` | Apple-1 profile JSON file |
| `--entry` | — | Set entry address (e.g. `0xE000` for BASIC, skips reset) |
| `--script` | — | Script file to feed as keyboard input |
| `--max-cycles` | 2000000 | Maximum CPU cycles before stopping |
| `--echo-input` | false | Echo typed characters to terminal |
| `--crlf` | apple1 | Line ending mode: `apple1` (LF→CR) or `native` |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Normal exit or script completed with output |
| 1 | Validation error or profile not found |
| 2 | Script completed with no output (max cycles exceeded) |

## Examples

### Start Woz Monitor directly

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic
```

### Start BASIC directly

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic --profile profiles/apple-1.json --entry 0xE000
```

### Run with custom profile

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic --profile my-apple1.json
```

### Script mode with custom cycle limit

```bash
dotnet run --project src/CmosCpu.Terminal -- apple1-basic --profile profiles/apple-1.json --script test.txt --max-cycles 500000
```

## Script File Format

Script files are plain text. Each character is queued to the Apple-1 PIA keyboard input as the CPU executes instructions. In `apple1` CR/LF mode, line feed characters (`\n`) are converted to carriage returns (`\r`) for Apple-1 compatibility.

Script mode exits when:
- The CPU halts
- Max cycles are reached (exit code 2 if no output was seen)
- All script characters have been sent and the CPU is idle

## How It Works

1. Profile is loaded, creating the `ComputerMachine` with PIA terminal device
2. ROM files are loaded from disk
3. If `--entry` is specified, PC is set directly; otherwise, CPU is reset (PC from vector)
4. The main loop:
   - Polls for keyboard input (interactive) or feeds script characters
   - Queues characters to `Apple1PiaTerminalDevice.QueueKey()`
   - Executes one CPU instruction via `Step()`
   - Checks for new terminal output via `ConsumeOutputSince()`
   - Displays output to console

## Known Limitations

1. **BASIC entry** — Starting BASIC with `--entry 0xE000` sets PC directly; Woz Monitor `E000R` command is not automatically issued
2. **No video rendering** — Terminal output is character-based; no video/character-generator emulation
3. **Keyboard input** — Characters are polled from stdin; no interrupt-driven keyboard
4. **Timing** — No real-time throttle; CPU runs as fast as possible
5. **Output buffering** — PIA output is consumed incrementally; may miss partial lines if not flushed
