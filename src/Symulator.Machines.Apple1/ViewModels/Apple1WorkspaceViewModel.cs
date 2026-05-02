using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Symulator.Application.Abstractions;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1WorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IMachineSession _session;
    private string _terminalOutput = string.Empty;
    private string _inputText = string.Empty;
    private string _statusText = "Ready";
    private string _modeText = "Unknown";

    public Apple1WorkspaceViewModel(IMachineSession session)
    {
        _session = session;
        BootMonCommand = new AsyncSessionCommand(() => _session.ExecuteMachineCommandAsync("apple1.boot.monitor"));
        BootBasicCommand = new AsyncSessionCommand(() => _session.ExecuteMachineCommandAsync("apple1.boot.basic"));
        ClearCommand = new AsyncSessionCommand(() => _session.ExecuteMachineCommandAsync("apple1.clear-terminal"));
        SendCommand = new AsyncSessionCommand(SendInput);
    }

    public string TerminalOutput
    {
        get => _terminalOutput;
        set { _terminalOutput = value; OnPropertyChanged(); }
    }

    public string InputText
    {
        get => _inputText;
        set { _inputText = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string ModeText
    {
        get => _modeText;
        set { _modeText = value; OnPropertyChanged(); }
    }

    public ICommand BootMonCommand { get; }
    public ICommand BootBasicCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand SendCommand { get; }

    private async Task SendInput()
    {
        await _session.SendInputAsync(InputText);
        InputText = string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

internal sealed class AsyncSessionCommand : ICommand
{
    private readonly Func<Task> _execute;
    private bool _isExecuting;

    public AsyncSessionCommand(Func<Task> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting;
    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _execute();
        }
        catch
        {
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
