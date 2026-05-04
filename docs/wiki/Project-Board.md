# Project Board

## Proponowany GitHub Projects

Nazwa:

```text
Symulator Roadmap
```

## Kolumny / statusy

| Status | Znaczenie |
|---|---|
| Inbox | nowe pomysły i nieprzetworzone zadania |
| Ready | gotowe do implementacji |
| In Progress | aktywnie realizowane |
| Review | gotowe do sprawdzenia lub PR |
| Done | zakończone i scalone |

## Pola niestandardowe

| Pole | Wartości |
|---|---|
| Area | CPU, Devices, Runtime, UI, Tests, Docs, Tooling |
| Priority | P0, P1, P2, P3 |
| Size | S, M, L |
| Risk | Low, Medium, High |

## Startowy backlog

1. Dodać testy integracyjne dla realnych `.solution.json`.
2. Rozdzielić `VisibleDeviceTypes` i `RuntimeDeviceTypes`.
3. Ulepszyć diagnostykę błędów solution manifest.
4. Zaimplementować RTC DS3231 core.
5. Dodać RTC direct bus adapter.
6. Dodać RTC I2C adapter.
7. Dodać UI/debug snapshot dla RTC.
8. Zweryfikować MOS 6502 testami referencyjnymi.
9. Przygotować MVP Z80.
10. Uporządkować dokumentację Wiki.

## Zasada pracy

Każde zadanie powinno mieć:

- jasne kryteria akceptacji,
- testy albo uzasadnienie braku testów,
- mały zakres zmian,
- link do PR po implementacji.
