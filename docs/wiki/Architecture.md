# Architecture

## Warstwy projektu

| Warstwa | Odpowiedzialność |
|---|---|
| Core | kontrakty CPU, bus, urządzeń i snapshotów |
| Bus / Memory | mapowanie pamięci i urządzeń MMIO |
| CPU | implementacje rdzeni procesorów |
| Devices | urządzenia I/O, LCD, UART, I2C, RTC |
| Runtime | składanie maszyn i profili |
| Application | kontrolery, sesje, katalogi i modele aplikacyjne |
| Machines | moduły konkretnych maszyn |
| Avalonia | frontend desktopowy |
| Tests | testy jednostkowe i integracyjne |

## Zasady projektowe

- urządzenia powinny mieć małe, testowalne adaptery,
- konfiguracja sprzętu powinna być deklaratywna,
- stan urządzenia powinien być rozdzielony od sposobu dostępu,
- UI nie powinno budować sprzętu bezpośrednio,
- runtime powinien być deterministyczny dla testów.

## Dynamiczne profile sprzętowe

Nowy mechanizm `.solution.json` pozwala wybrać program ASM oraz sprzęt, z którym program ma pracować.

Przepływ:

```text
ASM source + solution JSON
        -> SolutionDefinition
        -> HardwareSolutionBuilder
        -> HardwareRuntime
        -> MachineSession
        -> ViewModel / UI
```

## Główne ryzyka architektoniczne

- mieszanie widoczności UI z istnieniem urządzenia w runtime,
- zbyt szerokie fallbacki solution,
- brak testów dla realnych plików `.solution.json`,
- niespójność pomiędzy adapterami tego samego urządzenia.
