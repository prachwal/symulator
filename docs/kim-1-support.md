# KIM-1 Support

## Status obecny

- ROM-y KIM-1 są mapowane pod `0x1800` i `0x1C00`.
- Zakres I/O `0x1700-0x17FF` jest obsługiwany przez `Kim1Riot6530IoDevice`.
- UI pokazuje panel LED i keypad dla profilu KIM-1.
- Emulacja 6530 jest minimalna.

## Komponenty

### Kim1Riot6530IoDevice
- Klasa w `CmosCpu.Computer`.
- Obsługuje zakres adresów `0x1700-0x17FF`.
- Wewnętrzne rejestry I/O jako tablica 256 bajtów.
- Odczyt zwraca zapisany stan, zapis aktualizuje stan.
- `Version` rośnie przy zmianie wartości rejestru.
- Opcjonalnie przechowuje referencję do `Kim1KeypadState`.

### Kim1LedDisplayState
- Prezentacja 6-cyfrowego wyświetlacza 7-segmentowego.
- Domyślnie wyświetla `"------"`.
- `Version` śledzi zmiany.

### Kim1KeypadState
- Obsługuje klawisze: `0-9`, `A-F`, `AD`, `DA`, `GO`, `PC`, `+`.
- `PressKey(string)` i `ReleaseKey(string)`.
- `PressedKeys` jako `IReadOnlySet<string>`.

## Braki

- brak pełnego timera 6530,
- brak cycle-accurate I/O,
- mapowanie keypad/LED może wymagać doprecyzowania względem oryginalnego schematu,
- monitor ROM może wymagać dalszej kalibracji.

## Apple-1 PIA Terminal

Dodatkowo zaimplementowano `Apple1PiaTerminalDevice`:

- Obsługuje adresy `0xD010-0xD013`.
- `D010` — odczyt klawiatury,
- `D011` — status klawiatury (bit 7 = ready),
- `D012` — zapis znaku na terminal,
- `D013` — status wyświetlacza (zawsze ready).
- Terminal tekstowy 40x24 z obsługą `\r`.
- `QueueKey(char)` do kolejkowania znaków z UI.
