using System.Windows.Input;
using Avalonia.Threading;
using Symulator.Application.Abstractions;

namespace Symulator.Avalonia.ViewModels;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool> _canExecute;
    private readonly IUiErrorService? _errorService;
    private readonly string _commandName;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute, IUiErrorService? errorService = null, string commandName = "")
    {
        _execute = execute;
        _canExecute = canExecute;
        _errorService = errorService;
        _commandName = commandName;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute() && !_isExecuting;
    public async void Execute(object? parameter)
    {
        if (_isExecuting)
            return;

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute();
        }
        catch (Exception ex)
        {
            _errorService?.Report(ex, _commandName);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        Dispatcher.UIThread.Post(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
    }
}
