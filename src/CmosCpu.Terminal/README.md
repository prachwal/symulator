# Simulator.Terminal — Terminalowy klient symulatora

## Opis

Klient terminalowy (TUI) dla emulatora komputera retro MOS 6502. Umożliwia uruchamianie, zatrzymywanie, krokowanie CPU, podgląd rejestrów, pamięci, ekranu terminala oraz ładowanie programów — wszystko z poziomu konsoli.

## Tryby pracy

### Tryb interaktywny

```bash
dotnet run --project src/CmosCpu.Terminal -- interactive
```

Skróty klawiszowe:
- `R` — uruchom emulację
- `S` — krok (jedna instrukcja)
- `P` — wczytaj profil
- `M` — podgląd pamięci
- `D` — podgląd rejestrów i flag
- `T` — podgląd terminala I/O
- `K` — wysyłanie znaków do maszyny
- `L` — wczytaj plik BIN
- `H` — pomoc
- `Q/ESC` — wyjście

Podczas uruchomionej emulacji: `SPACE/ESC` pauzuje, `Q` wychodzi.

### Tryb wsadowy (batch)

```bash
# Stan emulatora
dotnet run --project src/CmosCpu.Terminal -- state --profile profiles/retro70-mos6502.json

# Krok
dotnet run --project src/CmosCpu.Terminal -- step --count 10 --profile profiles/retro70-mos6502.json

# Uruchomienie emulacji (Ctrl+C aby zatrzymać)
dotnet run --project src/CmosCpu.Terminal -- run --profile profiles/retro70-mos6502.json

# Podgląd pamięci
dotnet run --project src/CmosCpu.Terminal -- memory --from 0x0000 --length 256

# Podgląd rejestrów
dotnet run --project src/CmosCpu.Terminal -- registers

# Podgląd stosu
dotnet run --project src/CmosCpu.Terminal -- stack

# Zrzut pamięci do pliku
dotnet run --project src/CmosCpu.Terminal -- save --file dump.bin --from 0x0000 --length 4096

# Wczytanie pliku BIN
dotnet run --project src/CmosCpu.Terminal -- load --file roms/apple1.bin --addr 0xE000

# Kompilacja ASM
dotnet run --project src/CmosCpu.Terminal -- asm --file programs/blink.asm
```

### Kody wyjścia

| Kod | Znaczenie |
|-----|-----------|
| 0 | Sukces |
| 1 | Błąd walidacji argumentów |
| 2 | Błąd ładowania pliku |
| 3 | Błąd wykonania/emulacji |
| 4 | Błąd assemblera/kompilacji |
| 5 | Błąd nieobsługiwanej funkcji |

### Opcje globalne

- `--verbose`, `-v` — włącz szczegółowe logowanie
- `--no-color` — wyłącz kolory ANSI
- `--debug` — pokaż stack trace przy błędach

## Architektura

```
src/CmosCpu.Terminal/
├── Program.cs                  — punkt wejścia, konfiguracja NLog
├── AddressParser.cs            — parser adresów hex/dec
├── HexFormatter.cs             — formatowanie hex dump
├── NLog.config                 — konfiguracja logowania
├── Commands/
│   ├── CommandRouter.cs        — rejestracja i routing komend
│   └── BatchCommands.cs        — implementacje komend wsadowych
├── Session/
│   ├── ISimulatorSession.cs    — interfejs sesji symulatora
│   └── ComputerSession.cs      — implementacja (ComputerMachine + ComputerRunController)
├── Views/
│   ├── DashboardView.cs        — widok dashboardu
│   ├── RegistersView.cs        — widok rejestrów i flag
│   ├── MemoryView.cs           — widok pamięci (hex dump)
│   ├── StackView.cs            — widok stosu
│   ├── TerminalIoView.cs       — widok terminala znakowego
│   └── BreakpointsView.cs      — widok breakpointów
└── Tui/
    ├── TerminalApp.cs          — główna aplikacja terminalowa
    └── InteractiveShell.cs     — pętla interaktywna TUI
```

## Zależności

- `CmosCpu.Computer` — maszyna i sterowanie uruchamianiem
- `CmosCpu.Core` — podstawowe interfejsy
- `CmosCpu.Cpu` — CPU MOS 6502
- `CmosCpu.Assembler` — prosty assembler
- `Spectre.Console 0.49.1` — tabele, panele, markup
- `NLog 6.1.2` — logowanie do pliku

## Ograniczenia

- Breakpointy nie są zaimplementowane w rdzeniu CPU dla architektury Computer (wspiera je tylko stary CpuCore przez IDebugger).
- Disassembly wymaga disassemblera 6502 — obecnie niedostępny.
- Interactive TUI używa prostego odświeżania konsoli (`Console.Clear()`), co może migotać.
- Wymaga terminala obsługującego ANSI (większość nowoczesnych terminali).
