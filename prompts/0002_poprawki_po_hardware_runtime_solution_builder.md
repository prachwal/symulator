# Prompt 0002 — poprawki po implementacji HardwareRuntime i HardwareSolutionBuilder

## Kontekst

Commit do oceny:

```text
68986e084e5b19739e4c7b4fea029bc9ee9a427f
feat: implement MinimalBlink hardware runtime and solution builder
```

Commit idzie w dobrym kierunku i realizuje dużą część promptu `0001`:

- dodaje `MinimalBlinkHardwareRuntime`,
- dodaje `MinimalBlinkHardwareSolutionBuilder`,
- zaczyna budować sprzęt z `SolutionDefinition`,
- przepina `MinimalBlinkMachineSession` na `RebuildFromSolution(...)`,
- usuwa statyczne budowanie LCD/UART/I2C z domyślnego `Initialize()`,
- `LoadAndResetCompiled()` zaczyna używać `_selectedSolution`,
- `ResetAsync()` próbuje odbudować runtime z `_loadedSolution`.

Ten prompt opisuje poprawki wymagane przed dalszą rozbudową.

---

## Cel poprawki

Doprowadzić do stanu, w którym:

```text
.solution.json steruje nie tylko widocznością UI, ale też realnym runtime sprzętu.
```

Wymagany kontrakt:

```text
Select ASM:
  ładuje solution JSON do ViewModelu i przekazuje go do sesji

Compile:
  kompiluje kod, ale nie zmienia runtime

Load & Reset:
  buduje runtime z wybranego solution JSON
  ładuje program
  ustawia PC

Reset:
  odbudowuje runtime z ostatnio załadowanego solution JSON
  ponownie ładuje ostatni obraz programu

Step:
  nie robi auto-load
  wykonuje tylko już załadowany program
```

---

# Problem 1 — brak spięcia ViewModel → Session

## Objaw

`MinimalBlinkMachineSession` ma metodę:

```csharp
public void SetSelectedSolution(SolutionDefinition solution)
{
    _selectedSolution = solution;
}
```

ale `MinimalBlinkWorkspaceViewModel` prawdopodobnie ładuje manifest tylko do swojego prywatnego pola `_selectedSolution` i używa go do widoczności UI.

Jeśli ViewModel nie wywołuje `SetSelectedSolution(...)`, to `LoadAndResetCompiled()` użyje fallbacku:

```csharp
SolutionDefinition.CreateFallback(...)
```

Wtedy JSON steruje UI, ale nie runtime.

## Poprawka

W `MinimalBlinkWorkspaceViewModel.LoadSelectedSolutionAsync(...)` po poprawnym załadowaniu manifestu dodaj:

```csharp
if (_session is MinimalBlinkMachineSession asmSession)
{
    asmSession.SetSelectedSolution(_selectedSolution);
}
```

W gałęzi fallback analogicznie:

```csharp
_selectedSolution = SolutionDefinition.CreateFallback(program.Id, Path.GetFileName(program.FilePath));
SelectedSolutionSummary = $"Solution fallback: {program.Id}";
ApplySolutionToVisibleModules(_selectedSolution);

if (_session is MinimalBlinkMachineSession asmSession)
{
    asmSession.SetSelectedSolution(_selectedSolution);
}
```

## Kryterium akceptacji

Dla `blink.solution.json` `Load & Reset` buduje runtime według manifestu `blink`, a nie fallback.

---

# Problem 2 — `hd44780-pcf8574` może rzucić wyjątek

## Objaw

W builderze `hd44780-pcf8574` jest częściowo obsługiwany wewnątrz case `i2c-controller-mmio`:

```csharp
var pcfDevice = solution.Devices.FirstOrDefault(d => d.Type == "hd44780-pcf8574");
```

ale główna pętla `foreach` później dojdzie do urządzenia typu:

```text
hd44780-pcf8574
```

i trafi do `default`:

```csharp
throw new InvalidOperationException($"Unsupported device type: {device.Type}");
```

## Poprawka minimalna

Dodaj osobny case:

```csharp
case "hd44780-pcf8574":
    // handled together with i2c-controller-mmio
    break;
```

## Poprawka lepsza

Przebuduj builder na dwie fazy:

```text
Faza 1:
  budowa magistral/kontrolerów: i2c-controller-mmio, uart-mmio, hd44780-mmio

Faza 2:
  budowa urządzeń podrzędnych: hd44780-pcf8574
```

W tej iteracji wystarczy poprawka minimalna.

## Kryterium akceptacji

Manifest zawierający `i2c-controller-mmio` oraz `hd44780-pcf8574` nie rzuca `Unsupported device type`.

---

# Problem 3 — `MinimalBlinkHardwareRuntime.HasDevice("led-mmio")` zawsze zwraca true

## Objaw

Obecny kod:

```csharp
public bool HasDevice(string type) => type switch
{
    "cpu" => true,
    "led-mmio" => true,
    "uart-mmio" => Uart is not null,
    "hd44780-mmio" => Lcd is not null,
    "i2c-controller-mmio" => I2cController is not null,
    _ => false
};
```

Dla `hello-uart.solution.json` runtime może raportować, że LED istnieje, mimo że manifest go nie deklaruje.

To jest semantycznie błędne.

## Poprawka

Dodaj do runtime informację o typach urządzeń zadeklarowanych przez solucję:

```csharp
public IReadOnlySet<string> DeviceTypes { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
```

W builderze buduj set:

```csharp
var deviceTypes = solution.Devices
    .Where(d => d.Visible)
    .Select(d => d.Type)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
```

W runtime:

```csharp
public bool HasDevice(string type) => DeviceTypes.Contains(type);
```

Jeżeli potrzebujesz osobno wiedzieć, czy obiekt runtime istnieje, dodaj drugą metodę:

```csharp
public bool HasRuntimeInstance(string type) => type switch
{
    "uart-mmio" => Uart is not null,
    "hd44780-mmio" => Lcd is not null,
    "i2c-controller-mmio" => I2cController is not null,
    _ => HasDevice(type)
};
```

## Kryterium akceptacji

Dla `hello-uart.solution.json`:

```csharp
runtime.HasDevice("led-mmio").Should().BeFalse();
runtime.HasDevice("uart-mmio").Should().BeTrue();
```

Dla `blink.solution.json`:

```csharp
runtime.HasDevice("led-mmio").Should().BeTrue();
runtime.HasDevice("uart-mmio").Should().BeFalse();
```

---

# Problem 4 — `ResetAsync()` mógł uszkodzić legacy predefined programs

## Objaw

`ResetAsync()` obsługuje już tylko przypadek:

```csharp
_loadedAssemblyImage is not null && _loadedSolution is not null
```

Jeżeli użytkownik użyje `Load legacy program`, zwykły `Reset` może skończyć jako:

```text
Reset: no loaded program
```

Wcześniej był fallback do `MinimalBlinkPredefinedPrograms`.

## Poprawka

Jeżeli legacy programy nadal są widoczne w UI, przywróć gałąź:

```csharp
else if (_loadedProgramId is not null)
{
    var program = MinimalBlinkPredefinedPrograms.FindById(_loadedProgramId);
    if (program is not null)
    {
        Initialize();
        _memory!.ClearAll();
        _cpu!.Reset();
        _memory.Load(program.LoadAddress, program.Bytes);
        _cpu.PC = program.StartAddress;
        _isRunning = false;
        _status = $"Reset: {program.Name}, PC=${_cpu.PC:X4}";
        PublishSnapshot();
        return Task.CompletedTask;
    }
}
```

Uwaga: w tej gałęzi trzeba zachować minimalny albo legacy profile sprzętu. Nie wolno użyć ostatniego ASM solution, jeśli ładowany jest legacy program.

## Kryterium akceptacji

Po `Load legacy program` kliknięcie `Reset` ładuje ten sam legacy program, nie pokazuje `Reset: no loaded program`.

---

# Problem 5 — brak statusów CI

Dla commita:

```text
68986e084e5b19739e4c7b4fea029bc9ee9a427f
```

nie było widocznych statusów GitHub Actions/checks.

## Wymaganie

Po poprawkach uruchom lokalnie:

```bash
dotnet build
dotnet test
```

Jeżeli istnieje CI, sprawdź status PR/commita.

---

# Testy wymagane po tej poprawce

Dodaj lub popraw testy MSTest/FluentAssertions.

## MinimalBlinkWorkspaceViewModelTests

```text
SelectedAsmProgram_LoadsSolutionAndPassesItToSession
SelectedAsmProgram_Blink_ShowsCpuAndLedOnly
SelectedAsmProgram_HelloUart_ShowsCpuAndUartOnly
```

Jeżeli trudno bezpośrednio sprawdzić prywatne `_selectedSolution` w sesji, dodaj publiczny/internal test-only accessor albo sprawdzaj efekt po `Load & Reset`.

## MinimalBlinkHardwareSolutionBuilderTests

```text
Build_Blink_HasLedAndNoUart
Build_HelloUart_HasUartAndNoLed
Build_I2cWithPcf8574_DoesNotThrowUnsupportedDeviceType
Build_UnknownDeviceType_Throws
```

## MinimalBlinkMachineSessionTests

```text
LoadAndResetCompiled_UsesSelectedSolution
Reset_AfterLoadedAssembly_RebuildsSameSolution
Reset_AfterLegacyProgram_ReloadsLegacyProgram
Step_WithoutLoadedProgram_DoesNotAutoLoadDefault
Run_WithoutLoadedProgram_DoesNotAutoLoadDefault
```

---

# Kolejność implementacji

1. Popraw `MinimalBlinkWorkspaceViewModel.LoadSelectedSolutionAsync(...)`, aby wywoływał `MinimalBlinkMachineSession.SetSelectedSolution(...)`.
2. Popraw `MinimalBlinkHardwareSolutionBuilder`, aby `hd44780-pcf8574` nie wpadał w `default`.
3. Popraw `MinimalBlinkHardwareRuntime.HasDevice(...)`, żeby bazował na deklaracji z manifestu, a nie na stałej wartości `true` dla LED.
4. Przywróć poprawny `ResetAsync()` dla legacy predefined programs.
5. Dodaj testy.
6. Uruchom `dotnet build` i `dotnet test`.

---

# Zakazy

Nie dodawaj teraz pełnego dynamicznego routera MMIO.
Nie usuwaj legacy predefined programs.
Nie refaktoryzuj całej aplikacji shell.
Nie zmieniaj formatu wszystkich manifestów bez potrzeby.
Nie dodawaj nowych zależności NuGet.

---

# Oczekiwany efekt końcowy

Po poprawce:

- JSON steruje realnym runtime sprzętu,
- `blink` buduje CPU + LED,
- `hello-uart` buduje CPU + UART,
- runtime nie raportuje urządzeń nieobecnych w manifestach,
- `hd44780-pcf8574` nie powoduje błędu `Unsupported device type`,
- legacy reset nadal działa,
- `Step` i `Run` nadal nie robią auto-load,
- testy przechodzą.
