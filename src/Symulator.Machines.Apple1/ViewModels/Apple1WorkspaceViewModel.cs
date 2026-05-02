using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Symulator.Application.Abstractions;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1WorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IMachineSession _session;
    private readonly IMachineNotificationSink? _sink;
    private string _terminalOutput = string.Empty;
    private string _inputText = string.Empty;
    private string _statusText = "Not initialized";
    private string _modeText = "NotInitialized";

    public Apple1WorkspaceViewModel(IMachineSession session, IMachineNotificationSink? sink = null)
    {
        _session = session;
        _sink = sink;
        BootMonCommand = new AsyncResultCommand(() => _session.ExecuteMachineCommandAsync("apple1.boot.monitor"));
        BootBasicCommand = new AsyncResultCommand(() => _session.ExecuteMachineCommandAsync("apple1.boot.basic"));
        ClearCommand = new AsyncResultCommand(() => _session.ExecuteMachineCommandAsync("apple1.clear-terminal"));
        SendCommand = new AsyncResultCommand(SendInputAsync);
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

    private async Task<MachineCommandResult> SendInputAsync()
    {
        var text = InputText;
        InputText = string.Empty;
        var result = await _session.ExecuteMachineCommandAsync("apple1.send-line", text);
        HandleResult(result, "Send input");
        return result;
    }

    private void HandleResult(MachineCommandResult result, string context)
    {
        if (result.IsSuccess) return;

        var msg = $"[{context}] {result.ErrorMessage}";
        StatusText = msg;
        _sink?.Error(msg);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

internal sealed class AsyncResultCommand : ICommand
{
    private readonly Func<Task<MachineCommandResult>> _execute;
    private bool _isExecuting;

    public AsyncResultCommand(Func<Task<MachineCommandResult>> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting;
    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            var result = await _execute();
            if (!result.IsSuccess)
                System.Diagnostics.Debug.WriteLine($"Command failed: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Command exception: {ex}");
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
