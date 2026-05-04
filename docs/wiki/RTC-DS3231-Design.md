# RTC DS3231 Design

## Cel

Dodać moduł RTC dostępny jednocześnie przez:

- I2C jako DS3231/DS1307-compatible pod adresem `0x68`,
- direct bus jako memory-mapped I/O,
- opcjonalny tryb indeksowany `index + data`.

## Najważniejsza zasada

Stan czasu istnieje tylko raz w `RtcClockCore`. Adapter I2C i adapter direct bus muszą korzystać z tego samego rdzenia.

```text
I2C bus         -> RtcI2cDevice       ┐
                                       ├-> RtcClockCore
Direct bus/MMIO -> RtcBusMappedDevice ┘
```

## Zakres MVP

- rejestry czasu `0x00-0x06` w BCD,
- rejestry `Control` i `Status`,
- tryby czasu: `Host`, `Simulated`, `Manual`,
- direct bus: `Linear` i `Indexed`,
- I2C: pointer, sequential read/write, auto-increment, wrap-around,
- brak alarmów i IRQ w pierwszym etapie.

## Testy obowiązkowe

- BCD read/write,
- invalid BCD ignored,
- simulated tick advances time,
- manual tick does not advance time,
- linear direct bus mapping,
- indexed direct bus mapping,
- I2C pointer write,
- I2C sequential read/write.

## Dokumenty źródłowe

- `docs/rtc/rtc-ds3231-compatible-design.md`,
- `docs/rtc/rtc-implementation-plan.md`,
- `docs/rtc/rtc-profile-example.json`.
