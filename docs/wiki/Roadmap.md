# Roadmap

## Etap 1 — stabilizacja po merge ASM loader

- testy integracyjne dla prawdziwych plików `.solution.json`,
- walidacja komunikatów błędów manifestów,
- uporządkowanie `VisibleDeviceTypes` vs `RuntimeDeviceTypes`,
- dokumentacja workflow ASM.

## Etap 2 — RTC DS3231-compatible

- `RtcClockCore`,
- direct bus adapter,
- I2C adapter,
- testy rdzenia, direct bus i I2C,
- profil przykładowy,
- diagnostyka UI.

## Etap 3 — rozwój urządzeń I/O

- lepsza zgodność HD44780,
- rozszerzenie PCF8574,
- testy sekwencji LCD,
- snapshoty urządzeń dla debuggera.

## Etap 4 — procesory i maszyny

- hardening MOS 6502,
- testy referencyjne 6502,
- MVP Z80,
- profile Apple-1 i KIM-1,
- lepsza obsługa ROM i terminala.

## Etap 5 — UI i narzędzia

- panel runtime devices,
- debugger listing/PC marker,
- eksport/import profili,
- dokumentacja użytkownika.
