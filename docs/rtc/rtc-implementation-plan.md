# RTC implementation plan

## Faza 1: rdzeń RTC

Pliki docelowe sugerowane dla kodu:

```text
src/CmosCpu.Devices/Rtc/RtcClockCore.cs
src/CmosCpu.Devices/Rtc/RtcRegisterMap.cs
src/CmosCpu.Devices/Rtc/RtcTimeMode.cs
src/CmosCpu.Devices/Rtc/RtcOptions.cs
tests/CmosCpu.Devices.Tests/Rtc/RtcClockCoreTests.cs
```

Zakres:

- konwersja BCD `ToBcd()` / `FromBcd()`,
- rejestry `0x00-0x06`, `0x0E`, `0x0F`,
- tryby `host`, `simulated`, `manual`,
- walidacja zakresów,
- brak wyjątków przy odczycie/zapisie nieobsługiwanych rejestrów.

Testy:

- `ReadRegisters_ReturnsBcdEncodedTime`,
- `WriteRegisters_UpdatesTime`,
- `InvalidBcdWrite_IsIgnored`,
- `SimulatedMode_TickAdvancesTime`,
- `ManualMode_TickDoesNotAdvanceTime`.

## Faza 2: direct bus adapter

Pliki sugerowane:

```text
src/CmosCpu.Devices/Rtc/RtcBusMappedDevice.cs
tests/CmosCpu.Devices.Tests/Rtc/RtcBusMappedDeviceTests.cs
```

Zakres:

- tryb `linear`,
- tryb `indexed`,
- integracja z aktualnym interfejsem urządzeń memory-mapped,
- brak duplikacji stanu.

Testy:

- `LinearRead_MapsAddressToRegister`,
- `LinearWrite_MapsAddressToRegister`,
- `IndexedRead_UsesIndexAndDataPorts`,
- `IndexedWrite_UsesIndexAndDataPorts`,
- `OutOfRangeAddress_IsNotHandled`.

## Faza 3: I2C adapter

Pliki sugerowane:

```text
src/CmosCpu.Devices/Rtc/RtcI2cDevice.cs
tests/CmosCpu.Devices.Tests/Rtc/RtcI2cDeviceTests.cs
```

Zakres:

- adres `0x68`,
- zapis wskaźnika rejestru,
- sekwencyjny odczyt,
- sekwencyjny zapis,
- autoinkrementacja wskaźnika,
- wrap-around.

Testy:

- `I2cWriteSingleByte_SetsRegisterPointer`,
- `I2cSequentialRead_ReturnsTimeRegisters`,
- `I2cSequentialWrite_UpdatesRegisters`,
- `I2cRead_AutoIncrementsPointer`,
- `I2cRead_WrapsPointer`.

## Faza 4: profile i runtime

Zakres:

- obsługa konfiguracji JSON,
- rejestracja urządzenia przez builder maszyny,
- profil testowy z RTC pod `0xD100` i I2C `0x68`,
- diagnostyka snapshotu RTC.

Testy:

- profil ładuje RTC,
- direct bus odczytuje rejestry po załadowaniu profilu,
- I2C bus widzi adres `0x68`,
- snapshot pokazuje spójny czas.

## Faza 5: UI/debugger

Zakres:

- panel RTC w UI,
- widok rejestrów,
- ręczne ustawienie czasu,
- `Step +1s`,
- freeze/manual mode.

## Poza MVP

- alarm 1 / alarm 2,
- IRQ,
- SQW 1 Hz,
- pełna zgodność bitów DS3231,
- emulacja temperatury DS3231.
