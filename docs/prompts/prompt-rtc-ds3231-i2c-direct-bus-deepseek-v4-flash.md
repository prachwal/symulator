# Prompt: implementacja RTC DS3231-compatible dla symulatora

Jesteś agentem implementującym małą, dobrze przetestowaną zmianę w repozytorium C#/.NET symulatora retro komputerów. Pracuj etapami. Nie przebudowuj architektury bez potrzeby. Dopasuj się do istniejących interfejsów projektu.

## Cel

Zaimplementuj moduł RTC dostępny jednocześnie przez:

1. I2C jako DS3231/DS1307-compatible device pod adresem `0x68`,
2. direct bus jako memory-mapped I/O,
3. opcjonalny tryb indeksowany `index + data`.

Stan czasu musi istnieć tylko raz, w `RtcClockCore`. Adapter I2C i adapter direct bus mają używać tego samego obiektu rdzenia.

## Wymagania funkcjonalne

### Rejestry

Obsłuż minimum:

| Offset | Nazwa | Format |
|---:|---|---|
| `0x00` | Seconds | BCD |
| `0x01` | Minutes | BCD |
| `0x02` | Hours | BCD 24h |
| `0x03` | Day | BCD `1-7` |
| `0x04` | Date | BCD `01-31` |
| `0x05` | Month | BCD `01-12` |
| `0x06` | Year | BCD `00-99` |
| `0x0E` | Control | byte |
| `0x0F` | Status | byte |

Nieobsługiwane rejestry mają być bezpieczne: odczyt `0x00`, zapis bez wyjątku.

### Tryby czasu

Dodaj enum:

- `Host`,
- `Simulated`,
- `Manual`.

Zachowanie:

- `Host`: odczyt na podstawie providera czasu hosta, tick ignorowany.
- `Simulated`: `Tick(TimeSpan)` przesuwa czas.
- `Manual`: tick ignorowany, czas zmienia się tylko przez zapis rejestrów albo setter.

### Direct bus

Obsłuż dwa tryby:

- `Linear`: `baseAddress + offset` mapuje bezpośrednio na rejestr RTC.
- `Indexed`: `base + 0` to index, `base + 1` to data.

Przykładowa konfiguracja:

```json
{
  "type": "rtc",
  "model": "ds3231-compatible",
  "name": "rtc0",
  "timeMode": "simulated",
  "initialTime": "1977-04-11T00:00:00",
  "i2c": { "enabled": true, "address": "0x68" },
  "bus": { "enabled": true, "mode": "linear", "baseAddress": "0xD100", "size": "0x40" }
}
```

### I2C

Obsłuż:

- adres `0x68`,
- zapis wskaźnika rejestru,
- sekwencyjny odczyt,
- sekwencyjny zapis,
- autoinkrementację wskaźnika,
- wrap-around.

## Wymagania techniczne

- C# / .NET.
- Testy: MSTest, Moq, FluentAssertions.
- Logowanie, jeśli potrzebne, przez NLog.
- Nie dodawaj ciężkich zależności.
- Nie zmieniaj publicznych kontraktów szerzej niż konieczne.
- Nie implementuj alarmów i IRQ w pierwszym kroku, ale zostaw strukturę pod etap późniejszy.

## Sugerowane pliki

Dopasuj nazwy do repozytorium, ale preferuj:

```text
src/CmosCpu.Devices/Rtc/RtcClockCore.cs
src/CmosCpu.Devices/Rtc/RtcRegisterMap.cs
src/CmosCpu.Devices/Rtc/RtcTimeMode.cs
src/CmosCpu.Devices/Rtc/RtcOptions.cs
src/CmosCpu.Devices/Rtc/RtcBusMappedDevice.cs
src/CmosCpu.Devices/Rtc/RtcI2cDevice.cs
tests/CmosCpu.Devices.Tests/Rtc/RtcClockCoreTests.cs
tests/CmosCpu.Devices.Tests/Rtc/RtcBusMappedDeviceTests.cs
tests/CmosCpu.Devices.Tests/Rtc/RtcI2cDeviceTests.cs
```

## Testy obowiązkowe

Dodaj testy jednostkowe:

- `ReadRegisters_ReturnsBcdEncodedTime`,
- `WriteRegisters_UpdatesTime`,
- `InvalidBcdWrite_IsIgnored`,
- `SimulatedMode_TickAdvancesTime`,
- `ManualMode_TickDoesNotAdvanceTime`,
- `LinearRead_MapsAddressToRegister`,
- `LinearWrite_MapsAddressToRegister`,
- `IndexedRead_UsesIndexAndDataPorts`,
- `IndexedWrite_UsesIndexAndDataPorts`,
- `I2cWriteSingleByte_SetsRegisterPointer`,
- `I2cSequentialRead_ReturnsTimeRegisters`,
- `I2cSequentialWrite_UpdatesRegisters`.

## Kolejność pracy

1. Znajdź istniejące interfejsy urządzeń, szyny i tickowania.
2. Dodaj `RtcClockCore` i testy rdzenia.
3. Dodaj direct bus adapter i testy.
4. Dodaj I2C adapter i testy.
5. Dodaj konfigurację profilu.
6. Uruchom `dotnet test`.
7. Popraw błędy bez rozszerzania zakresu.

## Kryterium zakończenia

Zadanie jest zakończone, gdy:

- projekt się buduje,
- testy przechodzą,
- ten sam czas jest widoczny przez I2C i direct bus,
- dokumentacja konfiguracji RTC jest dostępna w `docs/rtc/`.
