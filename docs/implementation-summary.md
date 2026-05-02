# Podsumowanie implementacji — KIM-1 RIOT 6530 + CI/CD

## 1. KIM-1 RIOT 6530 — brakujące elementy

### Phase 1: Internal RAM 6530
- Dodano 128-bajtowy bufor RAM w `Kim1Riot6530IoDevice`
- Adresy 0x40-0xBF (offset względem startu urządzenia) obsługują RAM
- RAM jest niezależny od rejestrów I/O
- Testy: write/read, izolacja od portów

### Phase 2: Keypad scan coordination
- `Kim1KeypadState` rozszerzony o macierz row/column (4×4 hex + funkcje)
- `GetRowState(byte columnSelect)` zwraca stan wierszy dla wybranej kolumny
- `ComputerMachine.UpdateKim1KeypadMatrix()` synchronizuje kolumny z 6530-002 Port A do wierszy 6530-003 Port A
- Wywoływane po każdym `Step()` gdy oba RIOT istnieją

### Phase 3: PA7 edge detect
- `SetPortAInput()` wykrywa opadające zbocze na PA7 (1→0)
- Osobna flaga `_pa7IrqPending` — niezależna od timer IRQ
- `IrqPending` = (timerIrqPending && timerIrqEnabled) || pa7IrqPending

### Phase 4: Timer underflow hardening
- `IrqEnabled` → `TimerIrqEnabled` (jaśniejsza nazwa)
- Write 0x09 wyłącza IRQ ale nie czyści flagi underflow
- Ponowne załadowanie timera (write 0x04-0x08) czyści underflow i IRQ
- Odczyt flag register (0x05/0x06) czyści tylko timer IRQ/underflow, nie PA7 IRQ

### Phase 5: Diagnostics update
- `kim1-io` pokazuje: RAM[0x40], PA7 IRQ, Timer IRQ raw/enabled, IRQ Line

### Testy: 20 nowych (Computer.Tests → z 24 do 44)

## 2. CI/CD — GitHub Actions

- Plik `.github/workflows/dotnet-ci.yml`
- Trigger: push/pr na main/master + workflow_dispatch
- .NET SDK 10.0.x, solution: `CmosCpuSimulator.slnx`
- Kroki: checkout → setup-dotnet → restore → build Release → test → upload results → publish terminal
- Artefakty: `test-results` (trx) + `terminal-release` (publish)

## Zmienione pliki — RIOT 6530

- `src/CmosCpu.Computer/Kim1Riot6530IoDevice.cs` — RAM, PA7 IRQ, timer hardening
- `src/CmosCpu.Computer/Kim1KeypadState.cs` — macierz row/column, GetRowState()
- `src/CmosCpu.Computer/ComputerMachine.cs` — UpdateKim1KeypadMatrix()
- `src/CmosCpu.Terminal/Commands/BatchCommands.cs` — rozszerzona diagnostyka
- `tests/CmosCpu.Computer.Tests/Kim1DeviceTests.cs` — 20 nowych testów

## Zmienione pliki — CI/CD

- `.github/workflows/dotnet-ci.yml`

## Wyniki testów (Release)

| Suita | Testy | Status |
|-------|-------|--------|
| Terminal | 70 | ✅ PASS |
| Blazor | 30 | ✅ PASS |
| Computer | 44 | ✅ PASS |
| **Razem** | **144** | **✅ PASS** |

## Profile — stan końcowy

| Profil | PC | Boot | Uwagi |
|--------|----|------|-------|
| Retro70 | 0xF000 | ✅ | Monitor wbudowany |
| Apple-1 | 0xFF00 | ✅ | Woz Monitor |
| KIM-1 | 0x1C22 | ✅ | RIOT 6530 timer/IRQ/porty + mirror |

## Znane ograniczenia

1. KIM-1 monitor wymaga pełnej obsługi keypad scan i LED display — obecnie emulacja jest warstwowa i niepełna
2. Timer RIOT: tylko one-shot (brak auto-reload)
3. PA7 edge detect: tylko opadające zbocze, zawsze włączone
4. Brak RAM 6530 w mapie pamięci profilu — jest w urządzeniu ale nie przez `MapRam()`
5. CI/CD: wymaga .NET 10.0 SDK na GitHub Actions (może być preview)
