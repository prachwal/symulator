using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using NLog;
using Symulator.Application.Abstractions;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1WorkspaceViewModel : INotifyPropertyChanged
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
        BootMonCommand = new AsyncResultCommand(() => _session.ExecuteMachineCommandAsync("apple1.boot.monitor"), HandleResult, _sink, "Boot MON");
        BootBasicCommand = new AsyncResultCommand(() => _session.ExecuteMachineCommandAsync("apple1.boot.basic"), HandleResult, _sink, "Boot BASIC");
        ClearCommand = new AsyncResultCommand(() => _session.ExecuteMachineCommandAsync("apple1.clear-terminal"), HandleResult, _sink, "Clear terminal");
        SendCommand = new AsyncResultCommand(SendInputAsync, HandleResult, _sink, "Send input");
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

    private Task<MachineCommandResult> SendInputAsync()
    {
        var text = InputText;
        Logger.Debug("Apple-1 workspace SendCommand input='{Text}'", text.Replace("\r", "\\r").Replace("\n", "\\n"));
        InputText = string.Empty;
        return _session.ExecuteMachineCommandAsync("apple1.send-line", text);
    }

    private void HandleResult(MachineCommandResult result, string context)
    {
        if (result.IsSuccess)
        {
            StatusText = context;
            return;
        }

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
    private readonly Action<MachineCommandResult, string> _onResult;
    private readonly IMachineNotificationSink? _sink;
    private readonly string _context;
    private bool _isExecuting;

    public AsyncResultCommand(Func<Task<MachineCommandResult>> execute, Action<MachineCommandResult, string> onResult, IMachineNotificationSink? sink, string context)
    {
        _execute = execute;
        _onResult = onResult;
        _sink = sink;
        _context = context;
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
            var result = await _execute();
            _onResult(result, _context);
        }
        catch (Exception ex)
        {
            _sink?.Error(ex, _context);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
