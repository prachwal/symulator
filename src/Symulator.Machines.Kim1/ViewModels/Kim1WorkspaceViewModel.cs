using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Symulator.Application.Abstractions;
using Symulator.Machines.Kim1.Models;
using Symulator.Machines.Kim1.Services;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1WorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IMachineSession _session;
    private readonly IMachineNotificationSink? _sink;
    private string _displayText = "------";
    private string _lastKey = string.Empty;
    private string _statusText = "Ready";
    private string _portA = "00";
    private string _portB = "00";
    private string _ddra = "00";
    private string _ddrb = "00";
    private Kim1PredefinedProgram? _selectedPredefinedProgram;

    public Kim1WorkspaceViewModel(IMachineSession session, IMachineNotificationSink? sink = null)
    {
        _session = session;
        _sink = sink;
        ResetCommand = new AsyncResultCommand(() => _session.ExecuteMachineCommandAsync("kim1.reset"), HandleResult, _sink, "Reset");
        KeyPressCommand = new AsyncResultCommand<string>(PressAndReleaseAsync, HandleResult, _sink, "Key press");
        PredefinedPrograms = Kim1PredefinedPrograms.All;
        SelectedPredefinedProgram = PredefinedPrograms.FirstOrDefault();
        LoadPredefinedProgramCommand = new AsyncResultCommand(LoadPredefinedProgramAsync, HandleResult, _sink, "Load program");
    }

    public string DisplayText
    {
        get => _displayText;
        set { _displayText = value; OnPropertyChanged(); }
    }

    public string LastKey
    {
        get => _lastKey;
        set { _lastKey = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string PortA { get => _portA; set { _portA = value; OnPropertyChanged(); } }
    public string PortB { get => _portB; set { _portB = value; OnPropertyChanged(); } }
    public string DDRA { get => _ddra; set { _ddra = value; OnPropertyChanged(); } }
    public string DDRB { get => _ddrb; set { _ddrb = value; OnPropertyChanged(); } }

    public ICommand ResetCommand { get; }
    public ICommand KeyPressCommand { get; }
    public ICommand LoadPredefinedProgramCommand { get; }

    public IReadOnlyList<Kim1PredefinedProgram> PredefinedPrograms { get; }

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

    private async Task<MachineCommandResult> LoadPredefinedProgramAsync()
    {
        if (SelectedPredefinedProgram is null)
            return MachineCommandResult.Failure("No predefined KIM-1 program selected.");

        return await _session.ExecuteMachineCommandAsync(
            "kim1.load-predefined-program",
            SelectedPredefinedProgram.Id);
    }

    private async Task<MachineCommandResult> PressAndReleaseAsync(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return MachineCommandResult.Failure("No key provided");

        LastKey = key;
        var press = await _session.ExecuteMachineCommandAsync("kim1.press-key", key);
        if (!press.IsSuccess)
            return press;

        await Task.Delay(100);
        var release = await _session.ExecuteMachineCommandAsync("kim1.release-key", key);
        return release;
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

internal sealed class AsyncResultCommand<T> : ICommand
{
    private readonly Func<T?, Task<MachineCommandResult>> _execute;
    private readonly Action<MachineCommandResult, string> _onResult;
    private readonly IMachineNotificationSink? _sink;
    private readonly string _context;
    private bool _isExecuting;

    public AsyncResultCommand(Func<T?, Task<MachineCommandResult>> execute, Action<MachineCommandResult, string> onResult, IMachineNotificationSink? sink, string context)
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
            var result = await _execute((T?)parameter);
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
