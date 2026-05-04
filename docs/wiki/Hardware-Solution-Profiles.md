# Hardware Solution Profiles

## Cel

Profil `.solution.json` opisuje program ASM, wymagany sprzęt oraz układ paneli UI.

## Typowy plik

```json
{
  "id": "hello-lcd-i2c",
  "name": "Hello LCD via PCF8574",
  "source": "hello-lcd-i2c.asm",
  "entryPoint": "0x0100",
  "loadAddress": "0x0100",
  "devices": [],
  "ui": {
    "layout": {
      "left": ["program-loader"],
      "center": ["lcd0"],
      "right": ["cpu", "i2c0"],
      "bottom": ["asm-tabs"]
    }
  }
}
```

## Obsługiwane urządzenia Minimal Blink

| Type | Opis |
|---|---|
| `cpu` | CPU Minimal Blink |
| `led-mmio` | LED przez memory-mapped I/O |
| `uart-mmio` | UART terminal |
| `hd44780-mmio` | LCD HD44780 przez bezpośredni MMIO |
| `i2c-controller-mmio` | kontroler I2C przez MMIO |
| `hd44780-pcf8574` | LCD HD44780 przez PCF8574 na I2C |

## Reguły walidacji

- `hd44780-pcf8574` wymaga `i2c-controller-mmio`,
- nieznany typ urządzenia powinien zakończyć budowę runtime błędem,
- adresy MMIO powinny być zgodne z obsługiwanymi stałymi maszyny,
- `visible` opisuje widoczność UI, a niekoniecznie fizyczną obecność urządzenia.

## Rekomendowane testy

- ładowanie prawdziwych plików `programs/asm/*.solution.json`,
- budowa runtime dla LCD direct,
- budowa runtime dla LCD przez I2C,
- budowa runtime dla UART,
- walidacja błędnych zależności urządzeń.
