using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Threading;
using Symulator.Application.Abstractions;

namespace Symulator.Avalonia.ViewModels;

public sealed class ErrorWindowViewModel : ViewModelBase
{
    private readonly IUiErrorService _errorService;
    private readonly string _logsPath;
    private int _errorCount;

    public ObservableCollection<UiErrorEntry> ErrorEntries { get; } = new();

    public int ErrorCount
    {
        get => _errorCount;
        set => SetProperty(ref _errorCount, value);
    }

    public string LogsPath => _logsPath;

    public ICommand ClearCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }

    public ErrorWindowViewModel(IUiErrorService errorService, string logsPath)
    {
        _errorService = errorService;
        _logsPath = logsPath;
        ClearCommand = new RelayCommand(() =>
        {
            _errorService.Clear();
            ErrorEntries.Clear();
            ErrorCount = 0;
        }, () => true);
        OpenLogsFolderCommand = new RelayCommand(OpenLogsFolder, () => true);

        foreach (var e in _errorService.Errors)
            ErrorEntries.Add(e);

        ErrorCount = ErrorEntries.Count;
        _errorService.ErrorAdded += OnErrorAdded;
    }

    private void OnErrorAdded(object? sender, UiErrorEntry entry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ErrorEntries.Add(entry);
            ErrorCount = ErrorEntries.Count;
        });
    }

    private void OpenLogsFolder()
    {
        Directory.CreateDirectory(_logsPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = _logsPath,
            UseShellExecute = true
        });
    }
}
