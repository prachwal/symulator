# Minimal Blink Machine

## Rola

Minimal Blink Machine jest lekką maszyną testową do szybkiej walidacji CPU, pamięci, MMIO, assemblera i dynamicznego UI.

## Workflow

```text
Select ASM -> Compile -> Load & Reset -> Step/Run
```

## Aktualne urządzenia

| Urządzenie | Dostęp | Zastosowanie |
|---|---|---|
| LED | MMIO | najprostszy test zapisu na port |
| UART | MMIO | test wyjścia tekstowego |
| LCD HD44780 | MMIO | test panelu LCD direct |
| I2C Controller | MMIO | bramka do urządzeń I2C |
| PCF8574 LCD Backpack | I2C | LCD przez ekspander I2C |

## Testy krytyczne

- CPU wykonuje instrukcje minimalnego ISA,
- predefined programs nadal działają,
- ASM compile nie zmienia pamięci,
- Load & Reset buduje runtime z solution,
- Run dochodzi deterministycznie do HLT dla krótkich programów,
- PCF8574 bez I2C rzuca błąd walidacji.

## Kierunki rozwoju

- testy integracyjne na rzeczywistych plikach `.solution.json`,
- panel diagnostyczny runtime devices,
- rozdzielenie `VisibleDeviceTypes` i `RuntimeDeviceTypes`,
- bardziej szczegółowe błędy walidacji manifestów.
