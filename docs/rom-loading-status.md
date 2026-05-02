# Uruchamianie ROMów — status

## Przegląd

Profil komputerowy definiuje mapę pamięci (RAM, ROM, urządzenia I/O). Terminal klient
(`CmosCpu.Terminal`) po załadowaniu profilu próbuje automatycznie wczytać pliki ROM
wskazane w `"file"` w sekcji `memory.rom[]`.

## Flaki ROM — stan faktyczny

| Profil | ROM | Ścieżka | Status | Uwagi |
|--------|-----|---------|--------|-------|
| `retro70-mos6502.json` | Monitor ROM | `roms/retro70-monitor.bin` | ✅ Wczytany | Wbudowany monitor Retro70, CPU startuje z 0xF000 |
| `apple-1.json` | Woz Monitor | `roms/apple-1/Apple-1 ROM.bin` | ✅ Wczytany | 256 B @ 0xFF00, CPU startuje z 0xFF00 |
| `apple-1.json` | BASIC | `roms/apple-1/Apple-1 BASIC ROM.bin` | ✅ Wczytany | 4 KB @ 0xE000, opcjonalny |
| `apple-1.json` | Signetics 2513 | `roms/apple-1/Signetics 2513 Video ROM.bin` | ✅ Assets | 1 KB, nie CPU-addressable (asset graficzny) |
| `kim-1.json` | 6530-003 | `roms/kim-1/6530-003 fillerbyte00.bin` | ✅ Wczytany | 1 KB @ 0x1800 |
| `kim-1.json` | 6530-002 | `roms/kim-1/6530-002 fillerbyte00.bin` | ✅ Wczytany | 1 KB @ 0x1C00 |

## Znane problemy

### KIM-1: mirrorowanie wektorów

Na prawdziwym KIM-1 układ 6530-002 (ROM 0x1C00-0x1FFF) jest mirrorowany do
0xFC00-0xFFFF poprzez ignorowanie linii adresowej A15 przy aktywacji chip select.  
Oznacza to, że wektory przerwań zapisane na pozycjach 0x1FFA-0x1FFF w ROM-ie
są widoczne również pod adresami 0xFFFA-0xFFFF.

`ComputerMemoryBus` (w `CmosCpu.Computer`) nie implementuje tego mirroringu —
każdy zakres ROM/RAM jest mapowany dokładnie jeden raz. Adresy 0xFFFA-0xFFFF
pozostają w niezmapowanej przestrzeni i zwracają `OpenBusValue` (0xFF).

**Skutek:** po resecie CPU czyta wektor z 0xFFFC = 0xFF, PC = 0xFFFF.
CPU próbuje wykonać instrukcję z adresu 0xFFFF, co zwraca 0xFF (open bus)
i kończy się błędem "Unknown 6502 opcode: 0xFF".

**Potencjalne rozwiązania:**
1. Dodać mirrorowanie w `ComputerMachine.MapDevice()` dla profilu KIM-1
2. Albo dodać drugi zakres ROM w profilu kim-1.json pokrywający 0xFC00-0xFFFF
   (wymaga współdzielenia bufora danych z istniejącym zakresem 0x1C00-0x1FFF)

### Apple-1: terminal PIA

Apple-1 ma terminal PIA (6820/6821) pod adresami 0xD010-0xD013. Urządzenie
`Apple1PiaTerminalDevice` jest zaimplementowane i mapowane, ale Woz Monitor
oczekuje pełnej obsługi I/O (gotowość klawiatury, echo znaków). W praktyce
monitor się uruchamia (PC=0xFF00) i wyświetla kursor, ale interakcja przez
terminal PIA może wymagać dalszej kalibracji.

### Retro70: w pełni funkcjonalny

Profil Retro70 jest w pełni funkcjonalny. Monitor ROM jest budowany z kodu
(`Retro70MonitorRomBuilder`) i nie wymaga zewnętrznych plików. CPU startuje
poprawnie z 0xF000, ekran tekstowy i klawiatura działają.

## Stan po poprawkach

Wszystkie trzy profile ładują się i bootują poprawnie:

| Profil | PC po resecie | Status | Uwagi |
|--------|-------------|--------|-------|
| Retro70 | 0xF000 | ✅ Działa | Monitor wbudowany, CPU bootuje |
| Apple-1 | 0xFF00 | ✅ Działa | Woz Monitor wczytany, PIA terminal zmapowany |
| KIM-1 | 0x1C22 | ✅ Działa | ROM mirrorowany (0x1C00→0xFC00), wektory poprawne |

KIM-1 nie startuje już z 0xFFFF (open bus). Mirrorowanie ROM 6530-002
z 0x1C00-0x1FFF na 0xFC00-0xFFFF zostało zaimplementowane przez
`ComputerMemoryBus.MapRomMirror()` z współdzielonym buforem danych.

## Weryfikacja

```bash
# Wszystkie trzy profile bootują poprawnie
dotnet run --project src/CmosCpu.Terminal -- state --profile profiles/retro70-mos6502.json
dotnet run --project src/CmosCpu.Terminal -- state --profile profiles/apple-1.json
dotnet run --project src/CmosCpu.Terminal -- state --profile profiles/kim-1.json

# Oczekiwane wyniki:
# Retro70: PC=0xF000
# Apple-1: PC=0xFF00
# KIM-1:   PC=0x1C22 (NIE 0xFFFF)
```
