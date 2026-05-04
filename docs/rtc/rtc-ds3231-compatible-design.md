# RTC DS3231-compatible: I2C + direct bus design

## Cel

Dodać do symulatora moduł zegara czasu rzeczywistego dostępny równolegle przez:

1. magistralę I2C jako urządzenie zgodne funkcjonalnie z DS3231/DS1307 pod adresem `0x68`,
2. bezpośrednie mapowanie na szynę danych/adresową jako memory-mapped I/O,
3. opcjonalny tryb indeksowany `index + data` dla maszyn z ograniczoną przestrzenią I/O.

Najważniejsza decyzja: stan czasu istnieje tylko raz, w `RtcClockCore`. Adapter I2C i adapter bezpośredni nie mogą utrzymywać osobnych kopii czasu.

```text
I2C bus        -> RtcI2cDevice       ┐
                                      ├-> RtcClockCore -> rejestry BCD
Memory/I/O bus -> RtcBusMappedDevice ┘
```

## Model zgodności

Profil bazowy: `ds3231-compatible`.

Zakres MVP:

| Offset | Nazwa | Format | Opis |
|---:|---|---|---|
| `0x00` | Seconds | BCD | `00-59` |
| `0x01` | Minutes | BCD | `00-59` |
| `0x02` | Hours | BCD | `00-23` |
| `0x03` | Day | BCD | `1-7` |
| `0x04` | Date | BCD | `01-31` |
| `0x05` | Month | BCD | `01-12` |
| `0x06` | Year | BCD | `00-99` |
| `0x0E` | Control | byte | pole kontrolne, w MVP zachowywane jako bajt |
| `0x0F` | Status | byte | status, w MVP zachowywany jako bajt |
| `0x20-0x3F` | NVRAM | byte | opcjonalna pamięć użytkownika |

Alarmy DS3231 (`0x07-0x0D`) zostawić jako etap późniejszy. W MVP odczyt niezaimplementowanych rejestrów powinien zwracać `0x00`, a zapis nie powinien przerywać emulacji.

## Tryby czasu

| Tryb | Zachowanie |
|---|---|
| `host` | odczyty bazują na czasie hosta z `IClock`/providerem czasu; `Tick()` ignorowany |
| `simulated` | czas przesuwa się przez `Tick(TimeSpan elapsed)` wywoływany z cykli CPU |
| `manual` | czas jest zamrożony; zmienia go tylko `SetCurrentTime()` albo zapis rejestrów |

## Direct bus mode

### Linear

Przy `baseAddress = 0xD100` offset rejestru jest równy `address - baseAddress`.

| Adres | Rejestr |
|---:|---|
| `0xD100` | Seconds |
| `0xD101` | Minutes |
| `0xD102` | Hours |
| `0xD103` | Day |
| `0xD104` | Date |
| `0xD105` | Month |
| `0xD106` | Year |
| `0xD10E` | Control |
| `0xD10F` | Status |

### Indexed

Wariant dwurejestrowy:

| Adres | Znaczenie |
|---:|---|
| `base + 0` | register index |
| `base + 1` | register data |

Sekwencja 6502:

```asm
LDA #$00
STA $D100   ; wybór Seconds
LDA $D101   ; odczyt Seconds BCD
```

## I2C mode

Adres domyślny: `0x68`.

Wymagane zachowania:

- zapis wskaźnika rejestru,
- odczyt sekwencyjny,
- zapis sekwencyjny,
- autoinkrementacja wskaźnika,
- wrap-around po końcu mapy rejestrów,
- ACK/NACK na poziomie istniejącej abstrakcji I2C.

Typowy odczyt:

```text
START
ADDR 0x68 WRITE
REGISTER_OFFSET 0x00
REPEATED START
ADDR 0x68 READ
READ seconds, minutes, hours, day, date, month, year
STOP
```

## Konfiguracja profilu

```json
{
  "type": "rtc",
  "model": "ds3231-compatible",
  "name": "rtc0",
  "timeMode": "simulated",
  "initialTime": "1977-04-11T00:00:00",
  "i2c": {
    "enabled": true,
    "address": "0x68"
  },
  "bus": {
    "enabled": true,
    "mode": "linear",
    "baseAddress": "0xD100",
    "size": "0x40"
  },
  "interrupt": {
    "enabled": false,
    "line": "IRQ"
  }
}
```

## Integracja z UI/debuggerem

Panel diagnostyczny powinien pokazywać:

- model,
- tryb czasu,
- adres I2C,
- bazę direct bus,
- aktualny czas,
- tabelę rejestrów `0x00-0x0F`,
- akcje: `Set host time`, `Freeze`, `Reset to initial`, `Step +1s`.

## Kryteria akceptacji

- Ten sam `RtcClockCore` jest widoczny przez I2C i direct bus.
- Rejestry czasu są kodowane w BCD.
- Tryb `simulated` reaguje na ticki emulacji.
- Tryb `manual` nie przesuwa czasu automatycznie.
- I2C obsługuje register pointer i sekwencyjny odczyt.
- Direct bus obsługuje tryb `linear` i `indexed`.
- Niepoprawny BCD nie psuje stanu zegara.
