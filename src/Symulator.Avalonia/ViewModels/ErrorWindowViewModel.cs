using System.Collections.ObjectModel;
using System.Windows.Input;
using Symulator.Application.Abstractions;

namespace Symulator.Avalonia.ViewModels;

public sealed class ErrorWindowViewModel : ViewModelBase
{
    private readonly IUiErrorService _errorService;
    private int _errorCount;

    public ObservableCollection<UiErrorEntry> ErrorEntries { get; } = new();

    public int ErrorCount
    {
        get => _errorCount;
        set => SetProperty(ref _errorCount, value);
    }

    public ICommand ClearCommand { get; }

    public ErrorWindowViewModel(IUiErrorService errorService)
    {
        _errorService = errorService;
        ClearCommand = new RelayCommand(() =>
        {
            _errorService.Clear();
            ErrorEntries.Clear();
            ErrorCount = 0;
        }, () => true);

        foreach (var e in _errorService.Errors)
            ErrorEntries.Add(e);

        _errorService.ErrorAdded += OnErrorAdded;
    }

    private void OnErrorAdded(object? sender, UiErrorEntry entry)
    {
        ErrorEntries.Add(entry);
        ErrorCount = ErrorEntries.Count;
    }
}
