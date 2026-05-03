# Prompt dla agenta: KIM-1 — predefiniowane programy testowe ładowane z GUI

## Rola agenta

Jesteś precyzyjnym wykonawcą zmian w projekcie C#/.NET 10. Pracuj małymi krokami, nie przebudowuj architektury bez potrzeby i nie zmieniaj CPU core, jeśli testy jednoznacznie tego nie wymagają.

Twoim zadaniem jest dodać do modułu KIM-1 możliwość ładowania predefiniowanych programów testowych z GUI Avalonia.

---

## Cel funkcjonalny

Po wybraniu maszyny KIM-1 użytkownik ma mieć w panelu KIM-1 sekcję:

```text
Predefined program
[ RAM write loop        v ]
[ Load program ]
```

Po kliknięciu `Load program` aplikacja ma:

1. znaleźć wybrany program po ID,
2. utworzyć maszynę KIM-1, jeśli jeszcze nie istnieje,
3. wgrać bajty programu do pamięci RAM,
4. ustawić PC na adres startowy programu,
5. opublikować snapshot,
6. pokazać status w GUI,
7. umożliwić wykonanie programu przez `Step` albo `Run`.

To jest funkcja developerska/testowa GUI. Program nie jest wpisywany przez keypad ani terminal. Bajty są bezpośrednio ładowane do pamięci emulowanej maszyny.

---

## Kontekst projektu

Projekt:

```text
C# / .NET 10
Avalonia UI
NLog
MSTest + Moq + FluentAssertions
```

Istotne lokalizacje:

```text
src/Symulator.Machines.Kim1
src/Symulator.Application
src/Symulator.Avalonia
tests/Symulator.Machines.Kim1.Tests
profiles/kim-1.json
```

Nie rób:

```text
- nie przywracaj Blazor/WPF/Console/TUI,
- nie zmieniaj CPU core bez dowodu,
- nie hardcoduj ścieżek absolutnych,
- nie używaj .Wait() ani .Result(),
- nie połykaj wyjątków pustym catch,
- nie uruchamiaj pełnego dotnet test, bo projekt ma znane testy wiszące,
- nie dodawaj loadera plików z dysku w tym zadaniu.
```

---

## Wymagany zakres zmian

### 1. Model programu predefiniowanego

Dodaj w projekcie `Symulator.Machines.Kim1` model:

```csharp
namespace Symulator.Machines.Kim1.Models;

public sealed record Kim1PredefinedProgram(
    string Id,
    string Name,
    string Description,
    ushort LoadAddress,
    ushort StartAddress,
    byte[] Bytes,
    string ExpectedResultDescription);
```

Jeżeli projekt ma już katalog `Models`, użyj go. Jeśli nie, utwórz go.

---

### 2. Katalog programów predefiniowanych

Dodaj klasę:

```csharp
using Symulator.Machines.Kim1.Models;

namespace Symulator.Machines.Kim1.Services;

public static class Kim1PredefinedPrograms
{
    private static readonly Kim1PredefinedProgram[] Programs =
    [
        new Kim1PredefinedProgram(
            Id: "ram-write-loop",
            Name: "RAM write loop",
            Description: "Writes $42 to zero-page RAM address $0000 and loops forever.",
            LoadAddress: 0x0200,
            StartAddress: 0x0200,
            Bytes: [0xA9, 0x42, 0x85, 0x00, 0x4C, 0x05, 0x02],
            ExpectedResultDescription: "After several CPU steps RAM[$0000] should contain $42 and PC should loop at $0205."),
    ];

    public static IReadOnlyList<Kim1PredefinedProgram> All => Programs;

    public static Kim1PredefinedProgram? FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return Programs.FirstOrDefault(p =>
            string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
```

Program `ram-write-loop` używa kodu 6502:

```asm
; start: $0200
LDA #$42
STA $0000
JMP $0205
```

Kod maszynowy:

```text
A9 42 85 00 4C 05 02
```

Znaczenie instrukcji:

```text
$0200: A9 42     LDA #$42
$0202: 85 00     STA $00
$0204: 4C 05 02  JMP $0205
```

Jeżeli `$0000` albo `$0200` koliduje z aktualną mapą pamięci KIM-1, sprawdź `profiles/kim-1.json` i wybierz bezpieczny adres RAM. Nie zgaduj. Dostosuj wtedy testy i opis programu.

---

### 3. Komenda sesji KIM-1

W `Kim1MachineSession.ExecuteMachineCommandAsync(...)` dodaj obsługę komendy:

```text
kim1.load-predefined-program
```

Parametr:

```csharp
string programId
```

Zachowanie:

```text
1. Jeśli parametr nie jest stringiem — zwróć failure.
2. Upewnij się, że maszyna KIM-1 istnieje.
3. Znajdź program przez Kim1PredefinedPrograms.FindById(programId).
4. Jeżeli programu nie ma — zwróć failure z ID programu.
5. Wgraj wszystkie bajty do pamięci od LoadAddress.
6. Ustaw CPU PC na StartAddress.
7. Opublikuj snapshot.
8. Ustaw czytelny status.
9. Zaloguj operację przez NLog.
10. Zwróć MachineCommandResult.Success().
```

Przykładowa implementacja — dopasuj do rzeczywistych nazw pól/metod w projekcie:

```csharp
case "kim1.load-predefined-program":
{
    if (parameter is not string programId)
        return MachineCommandResult.Failure("Predefined program id is required.");

    if (!EnsureMachineCreated())
        return MachineCommandResult.Failure("Cannot create KIM-1 machine.");

    var program = Kim1PredefinedPrograms.FindById(programId);
    if (program is null)
        return MachineCommandResult.Failure($"Unknown KIM-1 predefined program: {programId}");

    for (var i = 0; i < program.Bytes.Length; i++)
    {
        var address = (ushort)(program.LoadAddress + i);
        _machine!.Memory.WriteByte(address, program.Bytes[i]);
    }

    _machine!.Cpu.PC = program.StartAddress;

    PublishSnapshot();

    var message = $"Loaded predefined program: {program.Name} at ${program.StartAddress:X4}";
    StatusChanged?.Invoke(this, message);
    Logger.Info(
        "KIM-1 predefined program loaded: id={ProgramId}, load=0x{Load:X4}, start=0x{Start:X4}, bytes={ByteCount}",
        program.Id,
        program.LoadAddress,
        program.StartAddress,
        program.Bytes.Length);

    return MachineCommandResult.Success();
}
```

Jeżeli `Cpu.PC` nie ma publicznego settera, nie używaj refleksji. Dodaj kontrolowaną metodę w odpowiedniej warstwie, np.:

```csharp
_machine.SetProgramCounter(program.StartAddress);
```

albo użyj istniejącej metody, jeśli już jest.

Jeżeli `Memory.WriteByte(...)` nie istnieje, użyj istniejącego API magistrali/pamięci zgodnego z architekturą. Nie dodawaj hacków.

---

### 4. ViewModel GUI dla KIM-1

W `Kim1WorkspaceViewModel` dodaj właściwości:

```csharp
public IReadOnlyList<Kim1PredefinedProgram> PredefinedPrograms { get; }

private Kim1PredefinedProgram? _selectedPredefinedProgram;

public Kim1PredefinedProgram? SelectedPredefinedProgram
{
    get => _selectedPredefinedProgram;
    set
    {
        if (_selectedPredefinedProgram == value)
            return;

        _selectedPredefinedProgram = value;
        OnPropertyChanged();
    }
}

public ICommand LoadPredefinedProgramCommand { get; }
```

W konstruktorze:

```csharp
PredefinedPrograms = Kim1PredefinedPrograms.All;
SelectedPredefinedProgram = PredefinedPrograms.FirstOrDefault();
LoadPredefinedProgramCommand = new AsyncResultCommand(LoadPredefinedProgramAsync);
```

Jeżeli `AsyncResultCommand` ma inną nazwę lub jest zdefiniowany lokalnie w module, użyj istniejącego wzorca z `Kim1WorkspaceViewModel`.

Dodaj metodę:

```csharp
private async Task<MachineCommandResult> LoadPredefinedProgramAsync()
{
    if (SelectedPredefinedProgram is null)
        return MachineCommandResult.Failure("No predefined KIM-1 program selected.");

    var result = await _session.ExecuteMachineCommandAsync(
        "kim1.load-predefined-program",
        SelectedPredefinedProgram.Id);

    if (result.IsSuccess)
    {
        StatusText =
            $"Loaded: {SelectedPredefinedProgram.Name} at ${SelectedPredefinedProgram.StartAddress:X4}";
    }
    else
    {
        StatusText = result.ErrorMessage ?? "Failed to load predefined KIM-1 program.";
        _sink?.Error(StatusText);
    }

    return result;
}
```

Nie aktualizuj UI z wątku roboczego. Jeżeli ViewModel już używa dispatcherów, zachowaj istniejący wzorzec.

---

### 5. Widok Avalonia dla KIM-1

W `Kim1WorkspaceView.axaml` dodaj niewielką sekcję. Nie przebudowuj całego widoku.

Preferowany wariant:

```xml
<StackPanel Spacing="6" Margin="0,8,0,0">
    <TextBlock Text="Predefined program" FontWeight="SemiBold" />

    <ComboBox
        ItemsSource="{Binding PredefinedPrograms}"
        SelectedItem="{Binding SelectedPredefinedProgram}"
        MinWidth="220">
        <ComboBox.ItemTemplate>
            <DataTemplate>
                <StackPanel>
                    <TextBlock Text="{Binding Name}" />
                    <TextBlock
                        Text="{Binding Description}"
                        FontSize="11"
                        Foreground="#8A99A8"
                        TextWrapping="Wrap" />
                </StackPanel>
            </DataTemplate>
        </ComboBox.ItemTemplate>
    </ComboBox>

    <Button
        Content="Load program"
        Command="{Binding LoadPredefinedProgramCommand}" />
</StackPanel>
```

Jeżeli istnieją style/przyciski w tym widoku, dopasuj sekcję do obecnego stylu.

---

## Scenariusz użytkownika w GUI

Docelowe użycie:

```text
1. Uruchom Symulator.Avalonia.
2. Wybierz maszynę KIM-1.
3. W panelu KIM-1 znajdź sekcję "Predefined program".
4. Wybierz "RAM write loop".
5. Kliknij "Load program".
6. Status powinien pokazać: Loaded: RAM write loop at $0200.
7. Kliknij Step kilka razy.
8. Program powinien zapisać $42 do RAM[$0000].
9. PC powinien wejść w pętlę programu.
```

---

## Testy jednostkowe

Dodaj testy do `tests/Symulator.Machines.Kim1.Tests`.

Używaj:

```text
MSTest
FluentAssertions
Moq tylko jeśli potrzebny
```

### Test 1 — katalog zawiera program

```csharp
[TestMethod]
public void PredefinedPrograms_ShouldContainRamWriteLoop()
{
    var program = Kim1PredefinedPrograms.FindById("ram-write-loop");

    program.Should().NotBeNull();
    program!.Name.Should().Be("RAM write loop");
    program.LoadAddress.Should().Be(0x0200);
    program.StartAddress.Should().Be(0x0200);
    program.Bytes.Should().Equal(0xA9, 0x42, 0x85, 0x00, 0x4C, 0x05, 0x02);
}
```

### Test 2 — nieznany program zwraca failure

```csharp
[TestMethod]
public async Task LoadPredefinedProgram_UnknownId_ShouldReturnFailure()
{
    var session = new Kim1MachineSession();

    var result = await session.ExecuteMachineCommandAsync(
        "kim1.load-predefined-program",
        "missing-program");

    result.IsSuccess.Should().BeFalse();
    result.ErrorMessage.Should().Contain("missing-program");
}
```

### Test 3 — ładowanie ustawia PC

```csharp
[TestMethod]
public async Task LoadPredefinedProgram_ShouldSetProgramCounterToStartAddress()
{
    var session = new Kim1MachineSession();

    var result = await session.ExecuteMachineCommandAsync(
        "kim1.load-predefined-program",
        "ram-write-loop");

    result.IsSuccess.Should().BeTrue();

    var cpu = session.Current.Cpu;
    cpu.Should().NotBeNull();
    cpu!.Pc.Should().Be("0200");
}
```

Dopasuj porównanie `Pc` do realnego typu w `CpuStateSnapshot`. Jeżeli `Pc` jest ushort, użyj:

```csharp
cpu!.Pc.Should().Be(0x0200);
```

### Test 4 — wykonanie programu zapisuje wartość do RAM

Jeśli sesja lub maszyna ma publiczny odczyt pamięci, dodaj test:

```csharp
[TestMethod]
public async Task LoadedRamWriteLoop_ShouldWriteExpectedValueAfterSteps()
{
    var session = new Kim1MachineSession();

    var load = await session.ExecuteMachineCommandAsync(
        "kim1.load-predefined-program",
        "ram-write-loop");

    load.IsSuccess.Should().BeTrue();

    for (var i = 0; i < 10; i++)
        await session.StepInstructionAsync();

    // Użyj istniejącego API diagnostycznego, jeżeli istnieje.
    // Oczekiwany efekt:
    // RAM[$0000] == $42
}
```

Jeżeli nie ma publicznego odczytu pamięci, dodaj minimalny, testowalny mechanizm diagnostyczny w sesji KIM-1 albo factory test helper. Nie wystawiaj całej pamięci do UI bez potrzeby.

Przykład pomocniczego API tylko jeśli pasuje do architektury:

```csharp
internal byte ReadMemoryForDiagnostics(ushort address)
{
    if (_machine is null)
        throw new InvalidOperationException("KIM-1 machine is not initialized.");

    return _machine.Memory.ReadByte(address);
}
```

Jeżeli używasz `internal`, dodaj `InternalsVisibleTo` dla projektu testowego.

---

## Kryteria akceptacji

Zadanie jest zakończone, gdy:

```text
- KIM-1 ma widoczną sekcję "Predefined program" w GUI.
- Lista zawiera co najmniej "RAM write loop".
- Kliknięcie "Load program" ładuje bajty do pamięci.
- PC po załadowaniu wskazuje adres startowy programu.
- Status w GUI pokazuje załadowany program.
- Program można wykonać przez Step.
- Po kilku krokach program zapisuje oczekiwaną wartość do RAM.
- Błędny programId zwraca MachineCommandResult.Failure.
- Testy KIM-1 przechodzą.
- Nie zmieniono CPU core bez uzasadnienia.
```

---

## Komendy walidacyjne

Uruchom:

```bash
dotnet build CmosCpuSimulator.slnx
dotnet test tests/Symulator.Machines.Kim1.Tests --no-build
```

Nie uruchamiaj pełnego:

```bash
dotnet test
```

bo projekt ma znane testy, które mogą wisieć.

---

## Kolejność pracy

Wykonaj w tej kolejności:

```text
1. Sprawdź aktualną mapę pamięci KIM-1 w profiles/kim-1.json.
2. Dodaj Kim1PredefinedProgram.
3. Dodaj Kim1PredefinedPrograms z programem RAM write loop.
4. Dodaj komendę kim1.load-predefined-program w Kim1MachineSession.
5. Dodaj test katalogu programów.
6. Dodaj test nieznanego ID.
7. Dodaj test ładowania i PC.
8. Dodaj GUI: ComboBox + Load program.
9. Dodaj test wykonania programu, jeśli dostępny jest odczyt pamięci.
10. Zbuduj projekt i uruchom testy KIM-1.
```

---

## Uwagi implementacyjne

- Jeśli `$0200` nie jest RAM w profilu KIM-1, wybierz inny adres RAM i konsekwentnie popraw program, opis i testy.
- Jeśli zapis do `$0000` koliduje z rejestrami/urządzeniami, wybierz inny bezpieczny adres RAM.
- Jeśli KIM-1 ma własny monitor ROM i standardowy obszar programów użytkownika, preferuj ten obszar.
- Nie dodawaj importu programu z pliku. To osobne zadanie.
- Nie dodawaj assemblera. Programy predefiniowane są na razie tablicami bajtów.
- Nie fałszuj outputu UI. Stan ma wynikać z pamięci/CPU.
- Logowanie per załadowanie programu: `Info`.
- Szczegółowe bajty i adresy: `Debug`.
