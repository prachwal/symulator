# Hardware solution profiles

## Cel

Program demonstracyjny nie powinien wymuszać pełnej konfiguracji sprzętowej maszyny.

Przykład:

- `blink.asm` potrzebuje CPU, pamięci i LED.
- `hello-uart.asm` potrzebuje CPU, pamięci i UART.
- `lcd-hello.asm` potrzebuje CPU, pamięci i LCD 16x2.
- `i2c-scan.asm` potrzebuje CPU, pamięci, I2C i opcjonalnie UART do logowania.

Aktualny kierunek rozwoju powinien rozdzielić:

1. kod programu,
2. obraz po kompilacji,
3. definicję sprzętu wymaganego do uruchomienia programu,
4. layout widocznych modułów w UI.

## Problem obecny

`MinimalBlinkMachineSession` buduje obecnie zestaw urządzeń jako jeden stały wariant:

- CPU,
- pamięć,
- LED,
- LCD,
- I2C,
- UART,
- terminal.

To jest wygodne dla prototypu, ale złe dla dalszej emulacji, bo prosty program `blink` pokazuje i inicjalizuje urządzenia, których nie używa.

## Docelowy model

Wprowadzić `SolutionProfile` albo `HardwareSolutionProfile`.

Profil opisuje, jaki sprzęt ma zostać zbudowany dla konkretnego programu lub scenariusza.

Minimalny model:

```csharp
public sealed class HardwareSolutionProfile
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Cpu { get; init; } = "minimal-blink-cpu";
    public MemoryProfile Memory { get; init; } = new();
    public IReadOnlyList<DeviceProfile> Devices { get; init; } = [];
    public UiLayoutProfile Ui { get; init; } = new();
}

public sealed class DeviceProfile
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public ushort BaseAddress { get; init; }
    public Dictionary<string, string> Options { get; init; } = new();
}
```

## Plik manifestu programu

Każdy program `.asm` może mieć obok manifest `.json` albo `.yaml`.

Przykład:

```text
programs/asm/blink.asm
programs/asm/blink.solution.json
```

`blink.solution.json`:

```json
{
  "id": "blink",
  "displayName": "Blink LED",
  "entryPoint": "$0100",
  "hardware": {
    "cpu": "minimal-blink-cpu",
    "memory": {
      "ramStart": "$0000",
      "ramSize": "$8000"
    },
    "devices": [
      {
        "id": "led",
        "type": "led-mmio",
        "baseAddress": "$FF00"
      }
    ]
  },
  "ui": {
    "modules": ["cpu", "led"]
  }
}
```

W tym scenariuszu UI nie pokazuje LCD, UART ani I2C.

## Przykład: hello-uart

```json
{
  "id": "hello-uart",
  "displayName": "Hello UART Terminal",
  "entryPoint": "$0100",
  "hardware": {
    "cpu": "minimal-blink-cpu",
    "memory": {
      "ramStart": "$0000",
      "ramSize": "$8000"
    },
    "devices": [
      {
        "id": "uart0",
        "type": "uart-mmio",
        "baseAddress": "$FE40"
      }
    ]
  },
  "ui": {
    "modules": ["cpu", "uart0"]
  }
}
```

## Przykład: lcd-hello

```json
{
  "id": "lcd-hello",
  "displayName": "LCD Hello",
  "entryPoint": "$0100",
  "hardware": {
    "cpu": "minimal-blink-cpu",
    "memory": {
      "ramStart": "$0000",
      "ramSize": "$8000"
    },
    "devices": [
      {
        "id": "lcd0",
        "type": "hd44780-mmio",
        "baseAddress": "$FE00",
        "options": {
          "columns": "16",
          "rows": "2",
          "bus": "direct"
        }
      }
    ]
  },
  "ui": {
    "modules": ["cpu", "lcd0"]
  }
}
```

## Przykład: lcd przez I2C PCF8574

```json
{
  "id": "lcd-i2c-demo",
  "displayName": "LCD I2C Demo",
  "entryPoint": "$0100",
  "hardware": {
    "cpu": "minimal-blink-cpu",
    "memory": {
      "ramStart": "$0000",
      "ramSize": "$8000"
    },
    "devices": [
      {
        "id": "i2c0",
        "type": "i2c-controller-mmio",
        "baseAddress": "$FE30"
      },
      {
        "id": "lcd0",
        "type": "hd44780-pcf8574",
        "baseAddress": "$0027",
        "options": {
          "bus": "i2c0",
          "columns": "16",
          "rows": "2"
        }
      }
    ]
  },
  "ui": {
    "modules": ["cpu", "i2c0", "lcd0"]
  }
}
```

## Rejestr urządzeń

Potrzebny jest rejestr fabryk urządzeń:

```csharp
public interface IDeviceFactory
{
    string Type { get; }
    IHardwareDevice Create(DeviceProfile profile, HardwareBuildContext context);
}
```

Przykładowe typy:

| Type | Urządzenie |
|---|---|
| `led-mmio` | LED pod adresem MMIO |
| `uart-mmio` | UART + terminal buffer |
| `hd44780-mmio` | LCD direct MMIO |
| `i2c-controller-mmio` | kontroler I2C MMIO |
| `hd44780-pcf8574` | LCD przez backpack I2C |

## Layout UI z profilu

UI nie powinien pokazywać wszystkich znanych urządzeń, tylko urządzenia obecne w profilu.

Profil:

```json
"ui": {
  "modules": ["cpu", "led"]
}
```

powinien dać:

```text
CPU card
LED card
```

Profil:

```json
"ui": {
  "modules": ["cpu", "lcd0", "uart0"]
}
```

powinien dać:

```text
CPU card
LCD 16x2 card
UART terminal card
```

## Zasada komponentów sprzętowych

Każde urządzenie widoczne w UI powinno mieć własny moduł/kartę:

- `CpuModuleCard`,
- `LedModuleCard`,
- `Lcd16x2ModuleCard`,
- `UartTerminalModuleCard`,
- `I2cModuleCard`.

Karty powinny mieć spójne gabaryty. Bazowy rozmiar to karta LCD 16x2.

Wariant bazowy:

```text
Width: 340
Height: 118
Padding: 10
Border: #31404F
Background: #111820
```

Duże moduły, takie jak terminal albo edytor ASM, są osobnymi panelami roboczymi, a nie małymi kartami sprzętu.

## Kolejność implementacji

### Faza 1: manifesty programów

- Dodać model `ProgramSolutionManifest`.
- Dodać loader manifestów z `programs/asm/*.solution.json`.
- Po wyborze programu ASM wczytać manifest, jeśli istnieje.
- Jeśli manifest nie istnieje, użyć profilu domyślnego.

### Faza 2: budowanie sprzętu z profilu

- Dodać `HardwareSolutionBuilder`.
- Przenieść tworzenie LED/UART/LCD/I2C z `MinimalBlinkMachineSession.Initialize()` do buildera.
- Budować tylko urządzenia wymienione w profilu.

### Faza 3: UI dynamiczne

- Dodać kolekcję `HardwareModules` w ViewModelu.
- Dodać typy modułów UI.
- Pokazywać tylko moduły zadeklarowane w `ui.modules`.

### Faza 4: migracja programów

Dodać manifesty:

- `blink.solution.json` → CPU + LED,
- `hello-uart.solution.json` → CPU + UART,
- `lcd-hello.solution.json` → CPU + LCD,
- `i2c-scan.solution.json` → CPU + I2C + UART.

### Faza 5: testy

Testy MSTest/FluentAssertions:

- `BlinkManifest_LoadsOnlyCpuAndLed`,
- `HelloUartManifest_LoadsOnlyCpuAndUart`,
- `LcdManifest_LoadsLcdButNotUart`,
- `MissingManifest_UsesSafeDefaultProfile`,
- `HardwareSolutionBuilder_RejectsUnknownDeviceType`,
- `UiModules_ShowOnlyDeclaredDevices`.

## Kryteria akceptacji

- Wybranie `blink.asm` nie pokazuje LCD/UART/I2C.
- Wybranie `hello-uart.asm` pokazuje UART, ale nie LCD.
- Wybranie programu LCD pokazuje LCD 16x2.
- Zmiana programu nie ładuje sprzętu do uruchomionej maszyny bez `Load & Reset` albo jawnego `Apply hardware profile`.
- Build i testy przechodzą.

## Uwaga architektoniczna

Ten mechanizm jest przygotowaniem pod większe emulacje typu Apple-1, KIM-1, C64, ZX Spectrum i komputery hybrydowe. Program, maszyna i sprzęt muszą być rozdzielone, inaczej liczba kombinacji urządzeń szybko stanie się nieutrzymywalna.
