# Symulator — Wiki

## Cel projektu

`CmosCpu Simulator` to edukacyjny symulator retro-komputerów i prostych maszyn 8-bitowych napisany w C#/.NET. Projekt rozwija modularną architekturę CPU, pamięci, urządzeń I/O, profili maszyn oraz interfejsu Avalonia.

## Aktualny zakres

- modularny runtime maszyny,
- Minimal Blink Computer jako lekka maszyna testowa,
- dynamiczne profile `.solution.json`,
- assembler dla programów ASM,
- urządzenia MMIO: LED, UART, LCD HD44780, I2C, PCF8574,
- przygotowana dokumentacja RTC DS3231-compatible,
- profile i moduły dla maszyn retro: Apple-1, KIM-1,
- testy MSTest + FluentAssertions.

## Najważniejsze strony

| Strona | Opis |
|---|---|
| [Getting Started](Getting-Started.md) | Uruchomienie projektu, build i testy |
| [Architecture](Architecture.md) | Warstwy, moduły i odpowiedzialności |
| [Hardware Solution Profiles](Hardware-Solution-Profiles.md) | Format `.solution.json` i dynamiczne UI |
| [Minimal Blink Machine](Minimal-Blink-Machine.md) | Maszyna testowa, workflow ASM i urządzenia |
| [RTC DS3231 Design](RTC-DS3231-Design.md) | Projekt zegara czasu rzeczywistego |
| [Roadmap](Roadmap.md) | Kolejne etapy rozwoju |
| [Project Board](Project-Board.md) | Sugerowana konfiguracja GitHub Projects |

## Standard pracy

1. Zmiany robimy na branchach tematycznych.
2. Każda zmiana runtime powinna mieć testy.
3. Zmiany w UI powinny mieć opis workflow i ryzyk.
4. Merge do `main` dopiero po `dotnet test`.
5. Większe funkcje rozbijamy na małe, recenzowalne commity.
