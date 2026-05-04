# Getting Started

## Wymagania

- .NET SDK zgodny z projektem,
- Git,
- opcjonalnie IDE: Rider, Visual Studio albo VS Code,
- system Windows, Linux albo macOS.

## Build

```powershell
dotnet build
```

## Testy

```powershell
dotnet test
```

Oczekiwany wynik po ostatniej stabilizacji:

```text
Total Tests: 63
Passed: 63
Failed: 0
Skipped: 0
```

## Uruchomienie UI

```powershell
dotnet run --project src/Symulator.Avalonia
```

## Workflow dla programów ASM

W Minimal Blink UI obowiązuje przepływ:

```text
Select ASM -> Compile -> Load & Reset -> Step/Run
```

| Krok | Efekt |
|---|---|
| Select ASM | wybiera plik źródłowy i ładuje pasujący `.solution.json` |
| Compile | kompiluje ASM i generuje listing, bez zmiany pamięci |
| Load & Reset | buduje runtime z solution, ładuje program i ustawia PC |
| Step/Run | wykonuje program krokowo lub ciągle |
