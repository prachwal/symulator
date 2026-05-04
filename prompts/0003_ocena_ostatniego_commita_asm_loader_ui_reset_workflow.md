# 0003 — Ocena ostatniego commita na gałęzi `fix/asm-loader-ui-reset-workflow`

## Zakres oceny

Oceniany zakres dotyczy ostatniego commita na gałęzi:

```text
fix/asm-loader-ui-reset-workflow
```

Commit dodaje głównie przykłady ASM oraz pliki `.solution.json` dla LCD, LCD przez I2C/PCF8574 oraz zmienia workflow użytkownika w kierunku:

```text
Select → Compile → Load & Reset → Step/Run
```

Ocena koncentruje się na ryzykach technicznych, spójności architektury, workflow sesji, UI oraz jakości testów.

---

## Werdykt

Commit jest kierunkowo dobry, ale **nie powinien być mergowany bez poprawek**.

Zmiana idzie w dobrą stronę architektonicznie: odchodzi od twardo zakodowanych programów i zaczyna budować sprzęt na podstawie manifestów `.solution.json`. To jest właściwy kierunek dla dalszego rozwoju symulatora, szczególnie jeśli projekt ma obsługiwać różne profile sprzętowe, urządzenia MMIO, LCD, UART, I2C i kolejne maszyny.

Jednocześnie obecny stan zawiera kilka ryzyk funkcjonalnych:

- możliwy race condition przy ładowaniu manifestu solution,
- niespójność między `StepInstructionAsync` i `RunAsync`,
- błąd widoczności paneli w UI,
- brak walidacji zależności `hd44780-pcf8574` → `i2c-controller-mmio`,
- część testów jest pozorna albo ma mylące nazwy.

---

## Co commit robi dobrze

### 1. Dobry kierunek: ASM + `.solution.json`

Dodanie manifestów typu:

```text
hello-lcd.solution.json
hello-lcd-i2c.solution.json
hello-lcd-poll.solution.json
```

jest właściwym krokiem. Manifest opisuje:

- CPU,
- pamięć,
- urządzenia,
- adresy MMIO,
- układ UI.

To jest lepsze niż trzymanie konfiguracji sprzętu w UI, predefined programs albo sesji.

Docelowy model powinien wyglądać tak:

```text
program.asm
program.solution.json
        ↓
SolutionDefinition
        ↓
HardwareSolutionBuilder
        ↓
HardwareRuntime
        ↓
MachineSession
        ↓
WorkspaceViewModel / UI
```

### 2. Builder sprzętu jest właściwą abstrakcją

`MinimalBlinkHardwareSolutionBuilder` jest dobrym miejscem na budowanie runtime z deklaratywnego manifestu.

Obecnie builder obsługuje m.in.:

- `cpu`,
- `led-mmio`,
- `hd44780-mmio`,
- `i2c-controller-mmio`,
- `hd44780-pcf8574`,
- `uart-mmio`.

To dobrze separuje odpowiedzialności:

- manifest mówi, co ma istnieć,
- builder tworzy konkretne obiekty runtime,
- sesja używa gotowego runtime,
- UI tylko pokazuje stan.

### 3. Oddzielenie Compile od Load & Reset jest poprawne

Nowy workflow:

```text
Select → Compile → Load & Reset → Step/Run
```

jest poprawniejszy niż automatyczne ładowanie programu podczas kompilacji.

`Compile` powinien wyłącznie:

- sparsować ASM,
- wygenerować obraz programu,
- pokazać listing,
- zgłosić diagnostykę.

`Load & Reset` powinien:

- zbudować sprzęt z solution,
- wyczyścić pamięć,
- załadować obraz programu,
- zresetować CPU,
- ustawić PC na `StartAddress`.

---

## Problemy blokujące / wymagające poprawki

### 1. Race condition: solution ładuje się asynchronicznie bez oczekiwania

W `MinimalBlinkWorkspaceViewModel.SelectedAsmProgram` ustawienie programu odpala ładowanie solution w trybie fire-and-forget:

```csharp
_ = LoadSelectedSolutionAsync(value);
```

Problem: użytkownik może szybko kliknąć `Compile`, a potem `Load & Reset`, zanim `_selectedSolution` zostanie załadowane.

Wtedy `MinimalBlinkMachineSession.LoadAndResetCompiled()` użyje fallbacka:

```csharp
var solution = _selectedSolution
    ?? SolutionDefinition.CreateFallback(_compiledImage.ProgramId, _compiledImage.ProgramId + ".asm");
```

Fallback zawiera tylko CPU. Nie zawiera LCD, UART ani I2C.

#### Możliwy efekt

Program `hello-lcd.asm` albo `hello-lcd-i2c.asm` może zostać skompilowany, ale runtime zostanie zbudowany bez wymaganego sprzętu.

Objawy:

- LCD nie pojawi się w runtime,
- UI pokaże niepełny sprzęt,
- program wykona zapisy pod MMIO, ale efekt może nie być zgodny z oczekiwaniem,
- użytkownik zobaczy niespójny stan: manifest wybrany w UI, ale sprzęt z fallbacka.

#### Rekomendowana poprawka

Dodać do VM stan ładowania solution:

```csharp
public bool IsSolutionLoading { get; private set; }
public bool CanCompileAndLoad => !IsSolutionLoading && SelectedAsmProgram is not null;
```

Następnie:

- blokować `CompileCommand` i `LoadAndResetCommand`, dopóki solution się ładuje,
- po zakończeniu `LoadSelectedSolutionAsync` odświeżać `CanExecute`,
- opcjonalnie przenieść ładowanie solution do jawnej operacji `SelectProgramAsync`.

Alternatywnie `LoadAndResetCompiled()` powinno mieć dostęp do ścieżki programu i synchronicznie zagwarantować, że solution jest załadowane przed budową runtime.

---

### 2. `RunAsync` nie obsługuje legacy predefined programs

`StepInstructionAsync` dopuszcza wykonanie, gdy istnieje:

```csharp
_loadedAssemblyImage is not null || _loadedProgramId is not null
```

Natomiast `RunAsync` wymaga wyłącznie:

```csharp
_loadedAssemblyImage is not null
```

#### Problem

Po wykonaniu komendy:

```text
minimal-blink.load-blink
```

można wykonać `Step`, ale `Run` zgłosi:

```text
No program loaded. Use Compile and Load & Reset.
```

To jest regresja względem starszego workflow predefined programs.

#### Poprawka minimalna

Ujednolicić warunek w `RunAsync`:

```csharp
if (_cpu is null || _memory is null || (_loadedAssemblyImage is null && _loadedProgramId is null))
{
    _status = "No program loaded. Use Compile and Load & Reset.";
    PublishSnapshot();
    return;
}
```

#### Alternatywa architektoniczna

Usunąć legacy predefined workflow całkowicie, ale wtedy należy:

- usunąć albo przepisać `MinimalBlinkPredefinedPrograms`,
- usunąć komendę `minimal-blink.load-predefined-program`,
- przepisać testy,
- zapewnić odpowiedniki wszystkich programów w `programs/asm/*.asm` i `.solution.json`.

Na tym etapie bezpieczniejsza jest poprawka minimalna.

---

### 3. Błąd widoczności panelu `No center module`

W XAML istnieją panele w `Grid.Column="1"`:

- UART widoczny przy `ShowUartModule`,
- I2C widoczny przy `ShowI2cModule`,
- `No center module` widoczny przy `!ShowUartModule`.

Problem: dla programu I2C stan będzie prawdopodobnie taki:

```text
ShowI2cModule = true
ShowUartModule = false
```

Wtedy widoczny będzie zarówno panel I2C, jak i panel `No center module`.

#### Poprawka

Dodać właściwość w ViewModel:

```csharp
public bool ShowNoCenterModule => !ShowUartModule && !ShowI2cModule;
```

Następnie po każdej zmianie `ShowUartModule` i `ShowI2cModule` wywołać:

```csharp
OnPropertyChanged(nameof(ShowNoCenterModule));
```

W XAML użyć:

```xml
IsVisible="{Binding ShowNoCenterModule}"
```

zamiast:

```xml
IsVisible="{Binding !ShowUartModule}"
```

---

### 4. `hd44780-pcf8574` bez I2C nie jest błędem

W builderze `hd44780-pcf8574` jest obsługiwany tylko przy okazji budowy `i2c-controller-mmio`.

Jeżeli manifest zawiera:

```json
{
  "type": "hd44780-pcf8574"
}
```

ale nie zawiera:

```json
{
  "type": "i2c-controller-mmio"
}
```

to builder nie zbuduje poprawnego sprzętu, ale też nie zgłosi jednoznacznego błędu.

#### Poprawka

Po pętli budującej urządzenia dodać walidację zależności:

```csharp
if (solution.HasDevice("hd44780-pcf8574") && i2cBus is null)
{
    throw new InvalidOperationException(
        "Device 'hd44780-pcf8574' requires 'i2c-controller-mmio'.");
}
```

Warto też dodać test:

```csharp
[TestMethod]
public void Build_Pcf8574WithoutI2cController_ShouldThrow()
```

---

### 5. `Visible` miesza widoczność UI z dostępnością runtime

`MinimalBlinkHardwareRuntime.DeviceTypes` jest budowane tylko z urządzeń, które mają `Visible = true`.

To oznacza, że urządzenie niewidoczne w UI może być fizycznie zbudowane w runtime, ale `HasDevice(type)` zwróci `false`.

To może być mylące, bo są dwa różne pojęcia:

```text
urządzenie istnieje w runtime
urządzenie jest widoczne w UI
```

#### Rekomendacja

Rozdzielić to na dwie kolekcje:

```csharp
public IReadOnlySet<string> RuntimeDeviceTypes { get; init; }
public IReadOnlySet<string> VisibleDeviceTypes { get; init; }
```

albo zmienić nazwy metod:

```csharp
HasVisibleDevice(type)
HasRuntimeInstance(type)
```

Obecna metoda `HasDevice` jest semantycznie niejednoznaczna.

---

## Problemy w testach

### 1. Test `LoadPredefinedProgram_OutOfRange_ShouldReturnFailure` nie testuje out-of-range

Test tworzy lokalnie program `too-big`, ale go nie wstrzykuje do katalogu programów i finalnie sprawdza, że `led-on` ładuje się poprawnie.

To jest test pozorny.

#### Poprawka

Opcja A — usunąć test.

Opcja B — dodać możliwość wstrzyknięcia katalogu predefined programs do sesji.

Opcja C — przenieść walidację zakresu programu do osobnej metody i przetestować ją bez zależności od globalnego katalogu.

Przykład docelowy:

```csharp
[TestMethod]
public void ValidateProgramRange_OutOfRom_ShouldFail()
```

---

### 2. Test `Step_WithoutProgram_ShouldAutoLoadAndStep` ma nazwę sprzeczną z zachowaniem

Komentarz w teście mówi, że step bez programu nie powinien auto-loadować, ale nazwa testu mówi coś przeciwnego.

#### Poprawka

Zmienić nazwę na:

```csharp
Step_WithoutProgram_ShouldNotAutoLoadAndShouldNotThrow
```

albo usunąć duplikat, ponieważ istnieje już test o bardzo podobnym zakresie:

```csharp
Step_WithoutLoadedProgram_DoesNotAutoLoad
```

---

### 3. Nadmierne użycie refleksji do prywatnego `_status`

Kilka testów czyta prywatne pole `_status` przez refleksję.

To jest kruche. Zmiana nazwy pola albo refactoring sesji popsuje testy bez zmiany zachowania publicznego API.

#### Rekomendacja

Dodać publiczny odczyt statusu do snapshotu albo do kontrolowanego API testowego.

Przykład:

```csharp
public string Status => _status;
```

albo rozszerzyć `EmulatorStateSnapshot` o status tekstowy, jeśli to pasuje do architektury aplikacji.

---

## Brakujące testy, które należy dodać

### 1. Workflow solution + ASM + runtime

Dodać test integracyjny:

```text
Load ASM source
Load matching .solution.json
Compile
LoadAndReset
Verify runtime has expected devices
Step/Run
Verify LCD/UART/LED state
```

Przykład zakresu:

```csharp
[TestMethod]
public async Task HelloLcdI2c_LoadAndReset_ShouldBuildI2cAndPcf8574Runtime()
```

### 2. Race condition solution loading

Dodać test lub refactoring, który wymusza deterministyczne ładowanie solution.

Jeżeli zostanie wprowadzona flaga `IsSolutionLoading`, test powinien sprawdzać:

```text
LoadAndResetCommand cannot execute while solution is loading
```

### 3. Panel widoczności centrum

Dodać test ViewModel:

```csharp
[TestMethod]
public void ApplySolution_I2cOnly_ShouldShowI2cAndNotShowNoCenterModule()
```

### 4. PCF8574 bez I2C

Dodać test buildera:

```csharp
[TestMethod]
public void Build_Pcf8574WithoutI2cController_ShouldThrow()
```

### 5. Run legacy predefined program

Jeżeli legacy workflow zostaje, dodać test:

```csharp
[TestMethod]
public async Task Run_AfterLoadPredefinedProgram_ShouldRun()
```

---

## Ryzyka średnie

### 1. Manifest deklaruje pola, których runtime jeszcze nie używa

`SolutionDefinition` zawiera m.in.:

- `EntryPoint`,
- `LoadAddress`,
- `Cpu.ClockHz`,
- `Memory.Ram`.

Obecnie najważniejszym realnie używanym elementem są urządzenia i układ UI.

To nie musi blokować commita, ale trzeba zdecydować, czy `.solution.json` ma być źródłem prawdy.

Jeżeli tak, to docelowo:

- `EntryPoint` powinien być walidowany względem ASM image,
- `LoadAddress` powinien być walidowany względem ASM image,
- `Memory.Ram` powinien wpływać na konfigurację pamięci,
- `Cpu.ClockHz` powinien wpływać na tempo emulacji albo metadata runtime.

### 2. `AddressParser.Parse16` nie wzbogaca błędów kontekstem

`AddressParser.Parse16` jest prosty i wystarczający na start, ale przy błędnym manifeście użytkownik może dostać mało kontekstowy wyjątek.

Warto docelowo raportować:

- nazwę pliku solution,
- id urządzenia,
- nazwę pola,
- błędną wartość.

Przykład komunikatu:

```text
Invalid address in hello-lcd.solution.json: device lcd0, field baseAddress, value '0xZZZZ'.
```

---

## Rekomendowana kolejność poprawek

### Krok 1 — poprawki blokujące

1. Ujednolicić warunek programu w `RunAsync`.
2. Naprawić widoczność `No center module`.
3. Dodać walidację `hd44780-pcf8574` bez `i2c-controller-mmio`.
4. Usunąć albo poprawić pozorne/mylące testy.

### Krok 2 — stabilizacja workflow

1. Usunąć fire-and-forget z ładowania solution albo zabezpieczyć komendy flagą `IsSolutionLoading`.
2. Dodać testy workflow `Compile → Load & Reset → Step/Run`.
3. Dodać testy runtime dla LCD direct, LCD I2C, UART i LED.

### Krok 3 — porządkowanie architektury

1. Rozdzielić pojęcia `RuntimeDeviceTypes` i `VisibleDeviceTypes`.
2. Zacząć używać `EntryPoint`, `LoadAddress`, `Memory.Ram`, `Cpu.ClockHz` albo oznaczyć je jako przyszłe pola.
3. Ulepszyć diagnostykę błędów manifestów.

---

## Minimalny zestaw zmian przed mergem

Przed mergem do `main` rekomendowany minimalny zakres to:

```text
[ ] RunAsync działa po legacy load-predefined-program albo legacy workflow jest usunięty.
[ ] I2C panel nie nakłada się z panelem No center module.
[ ] hd44780-pcf8574 bez i2c-controller-mmio rzuca czytelny wyjątek.
[ ] Test out-of-range predefined program jest usunięty albo realnie testuje walidację.
[ ] Test Step_WithoutProgram_ShouldAutoLoadAndStep ma poprawną nazwę albo jest usunięty.
[ ] Load & Reset nie może użyć fallback solution przez race condition po zmianie SelectedAsmProgram.
```

---

## Ocena końcowa

Commit jest dobrym etapem przejściowym w kierunku dynamicznych profili sprzętowych i programów ASM.

Największa wartość tej zmiany to wprowadzenie deklaratywnego mechanizmu `.solution.json` oraz jawnego workflow kompilacji i ładowania programu.

Największe ryzyko to niespójność między stanem wybranym w UI, asynchronicznie ładowanym solution i faktycznie zbudowanym runtime.

Rekomendacja:

```text
Nie mergować jeszcze do main.
Najpierw wykonać poprawki blokujące i dodać testy workflow.
```
