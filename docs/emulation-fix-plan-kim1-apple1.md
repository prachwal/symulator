# Plan napraw emulacji KIM-1 i Apple-1

## Audyt (Faza 1)

### Klasy odpowiedzialne za profile i ROM

| Klasa | Lokalizacja | Rola |
|-------|-------------|------|
| `ComputerProfile` | `CmosCpu.Core/ComputerProfileLoader.cs:13` | Model profilu (JSON) |
| `ComputerProfileMemorySection` | `CmosCpu.Core/ComputerProfileLoader.cs:30` | Pojedyncza sekcja ROM/RAM z `Start`, `Size`, `File`, `Mirrors` |
| `ComputerProfileLoader` | `CmosCpu.Core/ComputerProfileLoader.cs:58` | Ładowanie i walidacja JSON |
| `ComputerMachine` | `CmosCpu.Computer/ComputerMachine.cs:6` | Budowa mapy pamięci z profilu |
| `ComputerMemoryBus` | `CmosCpu.Computer/ComputerMemoryBus.cs:5` | Mapowanie RAM/ROM/urządzeń, mirrorowanie ROM |
| `ComputerRunController` | `CmosCpu.Computer/ComputerRunController.cs:3` | Asynchroniczna pętla CPU z batch |
| `ComputerSession` | `CmosCpu.Terminal/Session/ComputerSession.cs:8` | Warstwa aplikacyjna terminala |
| `Apple1PiaTerminalDevice` | `CmosCpu.Computer/Apple1PiaTerminalDevice.cs:5` | Emulacja PIA 6820/6821 dla Apple-1 |
| `Kim1Riot6530IoDevice` | `CmosCpu.Computer/Kim1Riot6530IoDevice.cs:3` | Emulacja 6530 I/O dla KIM-1 |

### Przepływ resetu CPU

1. `ComputerSession.LoadProfile()` → `ComputerMachine` (konstruktor buduje mapę pamięci)
2. `TryLoadProfileRoms()` → ładuje pliki ROM z dysku przez `ComputerMemoryBus.LoadRom()`
3. `machine.Reset()` → `Mos6502Cpu.Reset()`:
   - Ustawia SP=0xFD, I=1, CycleCount=7
   - Czyta reset vector z 0xFFFC-0xFFFD
   - Ustawia PC na wartość wektora

### Przepływ ładowania ROM

1. `TryLoadProfileRoms(machine, profile, profilePath?)`
2. Dla każdego `rom` w `profile.Memory.Rom` z polem `file`:
   - Rozwiąż ścieżkę przez `ResolveRomPath()` (katalog profilu → repo root → CWD)
   - Jeśli plik istnieje: wczytaj bajty, sprawdź rozmiar, wywołaj `machine.Memory.LoadRom(start, data)`
   - Jeśli nie istnieje: loguj warning i kontynuuj

### Brakujące elementy (przed naprawą)

#### KIM-1
1. **Brak mirrorowania ROM** — ROM 6530-002 (0x1C00-0x1FFF) nie był widoczny pod 0xFC00-0xFFFF
2. **Brak wektorów resetu** — CPU czytał open bus z 0xFFFC-0xFFFD → PC=0xFFFF
3. **Ścieżki ROM** — nazwy plików w profilu nie zgadzały się z rzeczywistymi (myślniki vs spacje)

#### Apple-1
1. **Stan początkowy** — Woz Monitor bootował (PC=0xFF00), ale wymagał terminala PIA
2. **Integracja terminala** — `Apple1PiaTerminalDevice` był zmapowany, ale nieprzetestowany z faktycznym monitorem

### Wykonane zmiany

## Faza 2 — Stabilne rozwiązywanie ścieżek ROM

- Dodano `ResolveRomPath()` w `ComputerSession`:
  1. Jeśli ścieżka absolutna → użyj bez zmian
  2. Jeśli profil załadowany z pliku → spróbuj względem katalogu profilu
  3. Jeśli nie znaleziono → spróbuj względem katalogu repozytorium (szuka `.slnx`)
  4. Fallback → `Directory.GetCurrentDirectory()`
- Dodano walidację rozmiaru ROM (warning jeśli krótszy/dłuższy niż `size`)
- Dodano parsowanie `rom.Start` z komunikatem błędu
- Dodano szczegółowy log NLog dla każdego ROM-u (oczekiwana ścieżka, exists, bajty, adres)

## Faza 3 — Mirrorowanie ROM (KIM-1)

- Dodano `ComputerProfileMemoryMirror` — nowy typ rekordu z `Start` i `Size`
- Dodano pole `Mirrors` do `ComputerProfileMemorySection`
- Dodano `ComputerMemoryBus.MapRomMirror(sourceStart, mirrorStart, size)`:
  - Wyszukuje istniejący zakres ROM zawierający `sourceStart`
  - Współdzieli ten sam bufor danych (nie kopiuje danych)
  - Mapuje mirror z read-only callbackami wskazującymi na współdzielony bufor
- Zaktualizowano `ComputerMachine.BuildMemoryMap()` — po mapowaniu ROM, jeśli ma `Mirrors`, mapuje mirror
- Dodano mirror w `kim-1.json`: `6530-002` ROM mirrorowany z 0x1C00 na 0xFC00

## Faza 4 — Apple-1 PIA terminal

- `Apple1PiaTerminalDevice` działał poprawnie, nie wymagał zmian
- Zweryfikowano zachowanie:
  - KBD status (0xD011): 0x80 gdy klawisz gotowy
  - KBD data (0xD010): zwraca znak, czyści flagę
  - DSP status (0xD013): zawsze 0x80 (gotowy)
  - DSP data (0xD012): zapis znaku do bufora, CR/LN flushuje linię

## Faza 5 — Run / Pause / Ctrl+C

- Dodano opcjonalny `CancellationToken?` do `ComputerRunController.StartAsync()`
- Zaktualizowano `ComputerSession.StartAsync()` i `ISimulatorSession`
- `HandleRun` w batch przekazuje token Ctrl+C do `session.StartAsync(cts.Token)`

## Faza 6 — Breakpointy

- Zdegradowano do komunikatu: "not implemented in CPU core yet"
- Usunięto obietnicę `add/remove` z helpa

## Faza 7 — Testy

### ComputerMemoryBusMirrorTests (5 testów)
- `MapRomMirror_ShouldShareData` — mirror odczytuje dane źródła
- `MapRomMirror_VectorArea_ShouldReflectSource` — wektory z 0x1FFC → 0xFFFC
- `MapRomMirror_PartialRange_ShouldWork` — częściowy zakres
- `MapRomMirror_WriteIsNoOp_ReadRemainsUnchanged` — ROM jest read-only
- `MapRomMirror_NoSource_DoesNotThrow` — brak źródła nie rzuca wyjątku

### ProfileIntegrationTests (15 testów)
- `Retro70_Profile_ShouldBootToF000`
- `Apple1_Profile_ShouldBootToFf00`
- `Kim1_Profile_ShouldNotBootToFfffOpenBus`
- `Kim1_Profile_ResetVector_ShouldComeFromMirroredRom`
- `Kim1_Profile_FirstStep_ShouldNotFailWithOpcodeFF`
- `Apple1_Profile_WozMonitor_ShouldStepSuccessfully`
- `Retro70_Profile_ShouldStepSuccessfully`
- `RomLoader_NotFound_DoesNotCrash`
- `Profile_FromFile_LoadsRomsSuccessfully`
- `Apple1Pia_KeyboardReady_ShouldBeSignaledAfterKeyQueued`
- `Apple1Pia_ReadKeyboardData_ShouldReturnQueuedCharacter`
- `Apple1Pia_ReadKeyboardData_ShouldClearReadyFlag`
- `Apple1Pia_WriteDisplayData_ShouldAppendToTerminalText`
- `Apple1Pia_DisplayStatus_ShouldBeReady`
- `Apple1Pia_Newline_ShouldFlushCurrentLine`

## Wyniki

```
dotnet build:   PASS (0 errors)
dotnet test:    PASS (70 terminal + 30 blazor + 5 mirror = 105 tests)

Retro70 state:  PC=0xF000  A=0x00  SP=0xFD  Cycles=7
Apple-1 state:  PC=0xFF00  A=0x00  SP=0xFD  Cycles=7
KIM-1 state:    PC=0x1C22  A=0x00  SP=0xFD  Cycles=7
```

## Znane ograniczenia

1. **KIM-1 monitor** — CPU startuje (PC=0x1C22), ale KIM-1 monitor wymaga pełnej emulacji 6530 I/O (timer, porty I/O). Obecna emulacja `Kim1Riot6530IoDevice` to tylko rejestry, bez timera.
2. **Apple-1 echo** — Woz Monitor odczytuje klawisz i wypisuje go na terminal; PIA działa, ale potrzeba testów interaktywnych.
3. **Breakpointy** — nie zaimplementowane w Mos6502Cpu; wymagałyby modyfikacji CPU.
4. **Retro70** — w pełni funkcjonalny, bez zmian.

---

## Podsumowanie

W ramach usprawnienia KIM-1 i Apple-1 wykonano:

### KIM-1 — mirrorowanie ROM
- Dodano `ComputerProfileMemoryMirror` — nowy typ profilu do definiowania mirrorów ROM
- Dodano `ComputerMemoryBus.MapRomMirror()` — współdzieli bufor danych źródła z mirrorem, nie kopiuje
- Zaktualizowano `ComputerMachine.BuildMemoryMap()` — automatycznie mapuje mirror po głównym ROM
- Dodano mirror do `profiles/kim-1.json`: ROM 6530-002 z 0x1C00-0x1FFF → 0xFC00-0xFFFF
- **Efekt**: CPU po resecie czyta wektor z mirroru (0xFFFC → 0x1C22 zamiast open bus 0xFFFF)

### Apple-1 — Woz Monitor
- Zweryfikowano działanie `Apple1PiaTerminalDevice`:
  - 0xD011 (KBDCR): 0x80 gdy klawisz gotowy ✅
  - 0xD010 (KBD): zwraca znak, czyści flagę ✅
  - 0xD013 (DSPCR): zawsze 0x80 (gotowy) ✅
  - 0xD012 (DSP): zapis znaku, CR flushuje linię ✅
- **Efekt**: Apple-1 bootuje do Woz Monitor z PC=0xFF00

### Stabilność
- `ResolveRomPath()`: ścieżki ROM rozwiązywane względem katalogu profilu → repo root → CWD
- Walidacja rozmiaru ROM (warning przy niezgodności z deklaracją w profilu)
- `ComputerRunController.StartAsync()` akceptuje `CancellationToken` — Ctrl+C w `run` poprawnie anuluje pętlę
- Breakpointy zdegradowane do komunikatu "not implemented"

### Testy
- **70** testów terminala (+15 integracyjnych)
- **5** testów mirrorowania w Computer.Tests
- **30** testów Blazor — bez zmian
- **Wszystkie przechodzą** (105 total)

### Profile — stan końcowy

| Komenda | PC | Status |
|---------|----|--------|
| `state --profile profiles/retro70-mos6502.json` | 0xF000 | ✅ Działa |
| `state --profile profiles/apple-1.json` | 0xFF00 | ✅ Działa |
| `state --profile profiles/kim-1.json` | 0x1C22 | ✅ Działa (było 0xFFFF) |
