# Prompt wykonawczy — pełna implementacja HardwareSolutionBuilder, dynamiczny sprzęt z JSON i dynamiczne moduły UI

## Rola agenta

Jesteś agentem implementacyjnym pracującym w repozytorium C#/.NET/Avalonia `prachwal/symulator`.

Twoim celem jest dokończenie architektury, w której program ASM nie uruchamia już jednej statycznej maszyny z wszystkimi urządzeniami, tylko korzysta z pliku `*.solution.json`, który definiuje:

- kod programu,
- adres ładowania,
- adres startowy,
- CPU,
- pamięć,
- urządzenia,
- adresy MMIO,
- widoczne moduły UI,
- pozycję modułów w layoucie.

Pracuj małymi, bezpiecznymi zmianami. Po każdej większej fazie uruchom:

```bash
dotnet build
dotnet test
```

Jeżeli nie możesz uruchomić testów, zostaw jasną informację w podsumowaniu.

---

## Stan obecny

Na branchu `fix/asm-loader-ui-reset-workflow` istnieje już pierwszy etap.

### Dodane modele

```text
src/Symulator.Application/Solutions/SolutionDefinition.cs
src/Symulator.Application/Solutions/SolutionDefinitionLoader.cs
```

Obecne klasy:

- `SolutionDefinition`
- `CpuDefinition`
- `MemoryDefinition`
- `MemoryRegionDefinition`
- `DeviceDefinition`
- `UiDefinition`
- `UiLayoutDefinition`
- `AddressParser`
- `SolutionDefinitionLoader`

### Dodane manifesty

```text
programs/asm/blink.solution.json
programs/asm/hello-uart.solution.json
programs/asm/i2c-scan.solution.json
```

### Częściowo zaimplementowane

`MinimalBlinkWorkspaceViewModel` ładuje `.solution.json` i ustawia właściwości:

```csharp
ShowCpuModule
ShowLedModule
ShowLcdModule
ShowUartModule
ShowI2cModule
SelectedSolutionSummary
```

`MinimalBlinkWorkspaceView.axaml` częściowo ukrywa/pokazuje moduły przez `IsVisible`.

`MinimalBlinkMachineSession` ma już poprawiony kierunek:

- `Step` nie powinien automatycznie ładować pierwszego programu,
- `Run` nie powinien automatycznie ładować pierwszego programu,
- `Load & Reset` ma być jedynym miejscem startu programu,
- `Reset` ma odtwarzać ostatnio załadowany program.

---

## Problem do rozwiązania

Obecnie JSON steruje głównie widocznością UI, ale runtime sprzętu jest nadal w dużej mierze statyczny.

Docelowo:

```text
blink.solution.json      -> buduje CPU + RAM + LED
hello-uart.solution.json -> buduje CPU + RAM + UART
i2c-scan.solution.json   -> buduje CPU + RAM + I2C + UART
lcd-hello.solution.json  -> buduje CPU + RAM + LCD
```

Nie wolno już zakładać, że `MinimalBlinkMachineSession.Initialize()` zawsze podłącza:

- LCD,
- UART,
- I2C,
- LED,
- terminal,
- PCF8574.

---

# Twardy kontrakt działania

## Select ASM

Po wyborze pliku ASM:

```text
- aktualizuje edytor ASM,
- ładuje odpowiadający *.solution.json,
- ustawia widoczne moduły UI według manifestu,
- NIE kompiluje,
- NIE ładuje programu do pamięci,
- NIE przebudowuje działającej maszyny.
```

## Compile

Po kliknięciu `Compile`:

```text
- kompiluje aktualny tekst edytora,
- tworzy compiled image,
- tworzy listing,
- NIE ładuje programu do pamięci,
- NIE resetuje CPU,
- NIE przebudowuje sprzętu.
```

## Load & Reset

Po kliknięciu `Load & Reset`:

```text
- używa ostatnio wybranego SolutionDefinition,
- buduje sprzęt dokładnie z JSON,
- czyści RAM/ROM/urządzenia,
- ładuje compiled image do pamięci,
- resetuje CPU,
- ustawia PC na StartAddress,
- zapisuje LoadedSolution + LoadedAssemblyImage,
- aktualizuje UI modułów,
- publikuje snapshot.
```

## Reset

Po kliknięciu `Reset`:

```text
- NIE wybiera pierwszego programu,
- NIE wybiera nowego JSON,
- używa ostatnio LoadedSolution,
- odbudowuje ten sam sprzęt z LoadedSolution,
- ładuje ten sam LoadedAssemblyImage,
- ustawia PC na StartAddress,
- publikuje snapshot.
```

## Step

Po kliknięciu `Step`:

```text
- NIE ładuje programu,
- NIE buduje nowej maszyny,
- NIE robi AutoLoadDefault,
- wykonuje jedną instrukcję, jeśli program jest załadowany,
- jeżeli program nie jest załadowany, ustawia status:
  "No program loaded. Use Compile and Load & Reset."
```

---

# Etap 1 — sanity check branch

## Zadania

1. Sprawdź stan branch:

```bash
git status
git branch --show-current
git log --oneline -10
```

2. Upewnij się, że pracujesz na:

```text
fix/asm-loader-ui-reset-workflow
```

3. Zaktualizuj branch względem `main`, jeżeli trzeba, ale nie niszcz zmian bez potwierdzenia.

4. Uruchom:

```bash
dotnet build
dotnet test
```

5. Zapisz obecne błędy kompilacji/testów przed zmianami.

## Kryterium akceptacji

Masz jasny punkt wyjścia i wiesz, czy projekt kompiluje się przed implementacją.

---

# Etap 2 — poprawa modeli SolutionDefinition

## Cel

Modele JSON mają być wystarczające do budowy runtime sprzętu i UI.

## Zadania

Sprawdź i ewentualnie popraw `SolutionDefinition.cs`.

Dodaj helpery, jeżeli ich brakuje:

```csharp
public ushort? TryGetAddress16()
public ushort? TryGetBaseAddress16()
public ushort? TryGetRegisterAddress16(string name)
```

Nie rób zbyt dużej abstrakcji. Celem jest bezpieczna implementacja minimalnego runtime buildera.

## Testy

Dodaj testy MSTest/FluentAssertions:

```text
AddressParser_Parses0xHex
AddressParser_ParsesDollarHex
AddressParser_ParsesDecimal
SolutionDefinitionLoader_LoadsBlinkManifest
SolutionDefinitionLoader_MissingManifest_ReturnsFallback
```

---

# Etap 3 — manifesty JSON programów

## Cel

Dodać komplet manifestów dla istniejących programów ASM.

## Istniejące

Sprawdź:

```text
programs/asm/blink.solution.json
programs/asm/hello-uart.solution.json
programs/asm/i2c-scan.solution.json
```

## Dodaj brakujący LCD demo, jeśli istnieje `lcd-hello.asm`

Jeżeli istnieje:

```text
programs/asm/lcd-hello.asm
```

dodaj:

```text
programs/asm/lcd-hello.solution.json
```

Jeżeli nie istnieje, nie twórz teraz nowego ASM bez potrzeby. Możesz zostawić manifest jako przykład tylko w docs.

## Standard adresów

W JSON trzymaj konsekwentnie:

```json
"0x0100"
"0xFF00"
"0xFE40"
```

## Manifest `blink`

Musi zawierać tylko:

```text
cpu
led0
```

Nie może zawierać:

```text
uart0
lcd0
i2c0
```

## Manifest `hello-uart`

Musi zawierać:

```text
cpu
uart0
```

Nie może zawierać:

```text
led0
lcd0
i2c0
```

## Manifest `i2c-scan`

Musi zawierać:

```text
cpu
i2c0
uart0
```

UART może być użyty jako log/output.

---

# Etap 4 — runtime sprzętu: HardwareRuntime

## Cel

Dodać obiekt reprezentujący zbudowaną konfigurację sprzętu.

## Nowy folder

```text
src/Symulator.Machines.MinimalBlink/Hardware/
```

## Nowa klasa

```csharp
public sealed class MinimalBlinkHardwareRuntime
{
    public MinimalBlinkCpu Cpu { get; init; } = default!;
    public MinimalBlinkMemory Memory { get; init; } = default!;

    public Hd44780Lcd? Lcd { get; init; }
    public MinimalBlinkLcdBuffer? LcdBuffer { get; init; }

    public I2cBus? I2cBus { get; init; }
    public MemoryMappedI2cController? I2cController { get; init; }

    public UartDevice? Uart { get; init; }
    public MemoryMappedUartAdapter? UartAdapter { get; init; }
    public TerminalBuffer? Terminal { get; init; }

    public IReadOnlyDictionary<string, object> Devices { get; init; } =
        new Dictionary<string, object>();

    public bool HasDevice(string id) => Devices.ContainsKey(id);
}
```

`MinimalBlinkMachineSession` może przejściowo zachować pola `_cpu`, `_memory`, `_lcd`, `_uart`, ale mają być ustawiane z `MinimalBlinkHardwareRuntime`.

---

# Etap 5 — HardwareSolutionBuilder

## Cel

Budować sprzęt z `SolutionDefinition`.

## Nowa klasa

```text
src/Symulator.Machines.MinimalBlink/Hardware/MinimalBlinkHardwareSolutionBuilder.cs
```

## Interfejs

```csharp
public sealed class MinimalBlinkHardwareSolutionBuilder
{
    public MinimalBlinkHardwareRuntime Build(SolutionDefinition solution)
    {
        // 1. create memory
        // 2. attach devices based on solution.Devices
        // 3. create cpu with memory
        // 4. return runtime
    }
}
```

## Minimalne wspierane typy urządzeń

### `cpu`

Nie buduje urządzenia MMIO, ale musi być uwzględniony w UI.

### `led-mmio`

MinimalBlinkMemory ma już LED pod stałym portem:

```csharp
MinimalBlinkMemory.LedPort // 0xFF00
```

Na tym etapie możesz wymagać, żeby JSON podawał `0xFF00`.

Dodaj walidację:

```csharp
if (AddressParser.Parse16(device.Address!) != MinimalBlinkMemory.LedPort)
    throw new InvalidOperationException("LED MMIO address must be 0xFF00 in current MinimalBlinkMemory.");
```

### `uart-mmio`

Tworzy:

```csharp
UartDevice
MemoryMappedUartAdapter
TerminalBuffer
```

Podłącza do pamięci przez:

```csharp
memory.AttachUart(adapter);
uart.ByteTransmitted += (_, args) => terminal.WriteByte(args.Value);
```

Waliduje `baseAddress == 0xFE40`, bo `MinimalBlinkMemory` ma aktualnie stały zakres `0xFE40-0xFE42`.

### `hd44780-mmio`

Tworzy:

```csharp
Hd44780Lcd
Hd44780DirectBusAdapter
MinimalBlinkLcdBuffer
```

Podłącza do pamięci przez:

```csharp
memory.AttachLcd(lcd, lcdBus);
```

Waliduje `baseAddress == 0xFE00`.

### `i2c-controller-mmio`

Tworzy:

```csharp
I2cBus
MemoryMappedI2cController
```

Podłącza:

```csharp
memory.AttachI2c(controller);
```

Waliduje `baseAddress == 0xFE30`.

### `hd44780-pcf8574`

Jeżeli `i2c0` istnieje:

```csharp
Pcf8574Hd44780Backpack
Hd44780Parallel4BitAdapter
```

Podłącza LCD przez I2C. To możesz zrobić w tej iteracji tylko jeśli obecne klasy są już stabilne. Jeśli to za duże, dodaj kontrolowany błąd:

```text
hd44780-pcf8574 is declared but not yet implemented by builder
```

## Nieznany typ urządzenia

Builder ma rzucić kontrolowany wyjątek:

```csharp
throw new InvalidOperationException($"Unsupported device type: {device.Type}");
```

## Testy

```text
Build_Blink_CreatesCpuMemoryAndNoUart
Build_HelloUart_CreatesTerminalAndUart
Build_Lcd_CreatesLcdBuffer
Build_I2cScan_CreatesI2cAndUart
Build_UnknownDeviceType_Throws
Build_InvalidLedAddress_Throws
```

---

# Etap 6 — przepięcie MinimalBlinkMachineSession na builder

## Cel

`MinimalBlinkMachineSession` ma budować sprzęt przez `MinimalBlinkHardwareSolutionBuilder`.

## Dodaj pola

```csharp
private readonly MinimalBlinkHardwareSolutionBuilder _hardwareBuilder = new();
private SolutionDefinition? _selectedSolution;
private SolutionDefinition? _loadedSolution;
private MinimalBlinkHardwareRuntime? _runtime;
```

## Dodaj metodę

```csharp
public void SetSelectedSolution(SolutionDefinition solution)
{
    _selectedSolution = solution;
}
```

ViewModel po załadowaniu JSON ma wywołać tę metodę, jeśli `_session is MinimalBlinkMachineSession`.

## Zastąp Initialize()

Obecne `Initialize()` buduje wszystko statycznie. Docelowo podziel:

```csharp
private void RebuildFromSolution(SolutionDefinition solution)
{
    _runtime = _hardwareBuilder.Build(solution);

    _memory = _runtime.Memory;
    _cpu = _runtime.Cpu;
    _lcd = _runtime.Lcd;
    _lcdBuffer = _runtime.LcdBuffer;
    _i2cBus = _runtime.I2cBus;
    _i2cController = _runtime.I2cController;
    _uart = _runtime.Uart;
    _uartAdapter = _runtime.UartAdapter;
    _terminal = _runtime.Terminal;

    _isInitialized = true;
}
```

`Load & Reset` i `Reset` mają używać `RebuildFromSolution(...)`.

## LoadAndResetCompiled

Docelowa logika:

```csharp
public void LoadAndResetCompiled()
{
    if (_compiledImage is null)
    {
        _status = "No compiled program. Use Compile first.";
        PublishSnapshot();
        return;
    }

    var solution = _selectedSolution
        ?? SolutionDefinition.CreateFallback(_compiledImage.ProgramId, _compiledImage.ProgramId + ".asm");

    RebuildFromSolution(solution);

    _memory.ClearAll();
    _memory.Load(_compiledImage.LoadAddress, _compiledImage.Bytes);

    _cpu.Reset();
    _cpu.PC = _compiledImage.StartAddress;

    _loadedSolution = solution;
    _loadedAssemblyImage = Clone(_compiledImage);
    _loadedProgramId = _compiledImage.ProgramId;

    _isRunning = false;
    _status = $"Loaded: {_compiledImage.ProgramId}, PC=${_cpu.PC:X4}";
    PublishSnapshot();
}
```

## ResetAsync

Docelowa logika:

```csharp
public Task ResetAsync(...)
{
    if (_loadedAssemblyImage is not null && _loadedSolution is not null)
    {
        RebuildFromSolution(_loadedSolution);

        _memory.ClearAll();
        _memory.Load(_loadedAssemblyImage.LoadAddress, _loadedAssemblyImage.Bytes);

        _cpu.Reset();
        _cpu.PC = _loadedAssemblyImage.StartAddress;

        _status = $"Reset: {_loadedAssemblyImage.ProgramId}, PC=${_cpu.PC:X4}";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    _status = "Reset: no loaded program";
    PublishSnapshot();
    return Task.CompletedTask;
}
```

## StepInstructionAsync

Nie wolno tworzyć fallback maszyny, która ukrywa błąd braku programu.

```csharp
public Task StepInstructionAsync(...)
{
    if (_cpu is null || _memory is null || (_loadedAssemblyImage is null && _loadedProgramId is null))
    {
        _status = "No program loaded. Use Compile and Load & Reset.";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    if (_cpu.PC == 0)
    {
        _status = "No executable PC. Use Load & Reset.";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    _cpu.Step();
    TickDevices();
    _status = _cpu.Halted ? "Halted" : "Stepped";
    PublishSnapshot();
    return Task.CompletedTask;
}
```

## TickDevices

Dodaj pomocniczą metodę:

```csharp
private void TickDevices()
{
    _lcd?.Tick(_cpu?.CycleCount ?? 0);
}
```

---

# Etap 7 — dynamiczne UI jako kolekcje modułów

## Cel

Obecne `ShowCpuModule`, `ShowLedModule`, itd. jest etapem przejściowym. Docelowo UI powinno mieć kolekcje modułów według regionów:

```csharp
ObservableCollection<HardwareModuleViewModel> LeftModules
ObservableCollection<HardwareModuleViewModel> CenterModules
ObservableCollection<HardwareModuleViewModel> RightModules
ObservableCollection<HardwareModuleViewModel> BottomModules
```

Jeżeli to za duży skok, wykonaj minimalną wersję:

```csharp
public bool ShowCpuModule { get; }
public bool ShowLedModule { get; }
public bool ShowLcdModule { get; }
public bool ShowUartModule { get; }
public bool ShowI2cModule { get; }
```

Ale dodaj strukturę pod przyszłe kolekcje:

```csharp
public sealed class HardwareModuleViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Region { get; init; } = "right";
}
```

---

# Etap 8 — XAML i layout

## Cel

Zachować działający layout, ale ukrywać urządzenia nieobecne w solucji.

## Wymagania

Dla `blink`:

```text
- widoczny ASM Program,
- widoczny CPU,
- widoczny LED,
- ukryty UART,
- ukryty LCD,
- ukryty I2C.
```

Dla `hello-uart`:

```text
- widoczny ASM Program,
- widoczny CPU,
- widoczny UART,
- ukryty LED,
- ukryty LCD,
- ukryty I2C.
```

Dla `i2c-scan`:

```text
- widoczny ASM Program,
- widoczny CPU,
- widoczny UART,
- widoczny I2C,
- ukryty LED,
- ukryty LCD.
```

## Uwaga o Avalonia

Nie używaj:

```xml
ColumnSpacing
RowSpacing
HorizontalScrollBarVisibility
```

W tym projekcie powodowały `AVLN2000`.

---

# Etap 9 — testy

## Minimalny zestaw testów

### SolutionDefinitionLoaderTests

```text
LoadForSourceAsync_Blink_LoadsBlinkSolution
LoadForSourceAsync_HelloUart_LoadsHelloUartSolution
LoadForSourceAsync_MissingManifest_ReturnsFallback
```

### AddressParserTests

```text
Parse16_Parses0xHex
Parse16_ParsesDollarHex
Parse16_ParsesDecimal
Parse16_InvalidValue_Throws
```

### MinimalBlinkHardwareSolutionBuilderTests

```text
Build_Blink_HasCpuMemoryAndLedOnly
Build_HelloUart_HasCpuMemoryAndUartTerminal
Build_I2cScan_HasCpuI2cAndUart
Build_UnknownDeviceType_Throws
Build_InvalidFixedMmioAddress_Throws
```

### MinimalBlinkMachineSessionTests

```text
Step_WithoutLoadedProgram_DoesNotAutoLoadDefault
Run_WithoutLoadedProgram_DoesNotAutoLoadDefault
LoadAndResetCompiled_UsesSelectedSolution
Reset_AfterLoadedAssembly_RebuildsSameSolution
Reset_DoesNotFallbackToFirstProgram
```

### MinimalBlinkWorkspaceViewModelTests

```text
SelectedAsmProgram_LoadsSolutionManifest
SelectedAsmProgram_Blink_ShowsCpuAndLedOnly
SelectedAsmProgram_HelloUart_ShowsCpuAndUartOnly
SelectedAsmProgram_I2cScan_ShowsCpuI2cAndUart
Compile_DoesNotChangeVisibleHardwareModules
```

---

# Etap 10 — migracja legacy predefined programs

Legacy programy nie powinny blokować nowego flow, ale nie usuwaj ich od razu.

Przycisk `Load legacy program` może nadal działać, ale:

- ładuje legacy bytes,
- czyści `_loadedAssemblyImage`,
- może używać legacy profilu sprzętu,
- nie miesza się z `*.solution.json`.

---

# Etap 11 — definicja adresów MMIO

JSON definiuje adresy urządzeń, ale obecny `MinimalBlinkMemory` ma stałe porty.

## Minimalna implementacja teraz

Waliduj, że JSON zgadza się z obecnymi stałymi:

| Device | JSON | Stała |
|---|---|---|
| LED | `0xFF00` | `MinimalBlinkMemory.LedPort` |
| LCD | `0xFE00` | `MinimalBlinkMemory.LcdCommandPort` |
| I2C | `0xFE30` | `MinimalBlinkMemory.I2cBase` |
| UART | `0xFE40` | `MinimalBlinkMemory.UartBase` |

Jeżeli JSON poda inny adres, rzucić wyjątek z jasnym komunikatem.

Nie refaktoryzuj całej pamięci do w pełni dynamicznego routera MMIO w tej iteracji.

---

# Etap 12 — przyszły MemoryMappedDeviceRouter

Nie implementuj tego, chyba że całość jest stabilna.

Docelowo:

```csharp
public interface IMemoryMappedDevice
{
    ushort Start { get; }
    ushort End { get; }
    byte Read(ushort address);
    void Write(ushort address, byte value);
}
```

To będzie następna iteracja.

---

# Etap 13 — dokumentacja

Zaktualizuj:

```text
docs/hardware-solution-profiles.md
```

Dopisz:

- co jest już zaimplementowane,
- co jest jeszcze etapem przyszłym,
- przykłady uruchomienia `blink`, `hello-uart`, `i2c-scan`,
- twardy kontrakt `Select -> Compile -> Load & Reset -> Step/Run`.

---

# Etap 14 — kryteria akceptacji ręcznej

## Blink

1. Wybierz `blink`.
2. UI pokazuje CPU i LED.
3. UI nie pokazuje LCD, UART, I2C.
4. Kliknij `Compile`.
5. Kliknij `Load & Reset`.
6. Kliknij `Step`.
7. PC zmienia się zgodnie z listingiem.
8. LED reaguje po wykonaniu `STA LED_PORT`.
9. `Reset` przywraca `blink`, a nie pierwszy/default.

## Hello UART

1. Wybierz `hello-uart`.
2. UI pokazuje CPU i UART.
3. UI nie pokazuje LED ani LCD.
4. Kliknij `Compile`.
5. Kliknij `Load & Reset`.
6. Kliknij `Run`.
7. Terminal pokazuje `Hello UART`.
8. `Reset` ładuje nadal `hello-uart`.

## I2C Scan

1. Wybierz `i2c-scan`.
2. UI pokazuje CPU, I2C i UART.
3. UI nie pokazuje LED ani LCD.
4. Kliknij `Compile`.
5. Kliknij `Load & Reset`.
6. Kliknij `Run`.
7. Terminal pokazuje wynik skanowania albo status programu.

## Brak programu

1. Uruchom aplikację.
2. Bez `Load & Reset` kliknij `Step`.
3. Nie ładuje się automatycznie `blink`.
4. Status pokazuje:
   `No program loaded. Use Compile and Load & Reset.`

---

# Zakazy w tej iteracji

Nie rób teraz:

- pełnego emulatora 6502,
- pełnego Apple-1,
- pełnego KIM-1,
- pełnego dynamicznego routera MMIO, jeśli nie jest konieczny,
- migracji całej aplikacji shell,
- zmiany stylu całej aplikacji,
- usuwania legacy predefined programs,
- dodawania nowych frameworków UI,
- dodawania nowych zależności NuGet bez potrzeby.

---

# Oczekiwany wynik końcowy

Po zakończeniu tej iteracji:

```text
program ASM + solution JSON -> dokładnie wymagany runtime sprzętu + właściwe UI
```

Minimalne efekty:

- `blink` nie pokazuje LCD/UART/I2C,
- `hello-uart` nie pokazuje LCD/LED,
- `i2c-scan` pokazuje I2C + UART,
- `Step` nie ładuje automatycznie pierwszego programu,
- `Run` nie ładuje automatycznie pierwszego programu,
- `Reset` odtwarza ostatnio załadowaną solucję,
- runtime urządzeń jest budowany przez `MinimalBlinkHardwareSolutionBuilder`,
- testy jednostkowe przechodzą.

---

# Sugerowana kolejność commitów

```text
feat: add solution manifest models and loader tests
feat: add minimal blink hardware runtime and builder
fix: make minimal blink session build hardware from solution
feat: bind solution manifests to workspace module visibility
test: cover solution-based hardware selection
docs: update hardware solution profile documentation
```
