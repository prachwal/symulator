using System.Windows.Input;

namespace CmosCpu.WpfApp.Commands;

public class AsyncRelayCommand : ICommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;
    private CancellationTokenSource? _cts;

    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            _isExecuting = value;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        IsExecuting = true;
        _cts = new CancellationTokenSource();
        try
        {
            await _execute(_cts.Token);
        }
        finally
        {
            IsExecuting = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }
}
