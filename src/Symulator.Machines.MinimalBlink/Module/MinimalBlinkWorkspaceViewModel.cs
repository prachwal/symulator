using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using Symulator.Application.Abstractions;
using Symulator.Machines.MinimalBlink.Models;
using Symulator.Machines.MinimalBlink.Programs;

namespace Symulator.Machines.MinimalBlink.Module;

public sealed class MinimalBlinkWorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IMachineSession _session;
    private string _statusText = "Ready";
    private string _pc = "$0000";
    private string _a = "$00";
    private bool _z;
    private ulong _cycles;
    private bool _halted;
    private bool _ledOn;
    private long _ledToggleCount;
    private byte _ledLastValue;
    private string? _lastError;
    private MinimalBlinkPredefinedProgram? _selectedProgram;
    private IBrush _ledColor = OffBrush;

    private static readonly IBrush OnBrush = new SolidColorBrush(Color.Parse("#4DFF88"));
    private static readonly IBrush OffBrush = new SolidColorBrush(Color.Parse("#1a3a2a"));
    private static readonly IBrush HaltedBrush = new SolidColorBrush(Color.Parse("#FF5C5C"));

    public MinimalBlinkWorkspaceViewModel(IMachineSession session)
    {
        _session = session;
        PredefinedPrograms = MinimalBlinkPredefinedPrograms.All;
        SelectedProgram = PredefinedPrograms.FirstOrDefault();
        LoadProgramCommand = new AsyncRelayCommand(LoadProgramAsync);
        ResetCommand = new AsyncRelayCommand(ResetAsync);
        StepCommand = new AsyncRelayCommand(StepAsync);
        RunCommand = new AsyncRelayCommand(RunAsync);
        PauseCommand = new AsyncRelayCommand(PauseAsync);
    }

    public IReadOnlyList<MinimalBlinkPredefinedProgram> PredefinedPrograms { get; }

    public MinimalBlinkPredefinedProgram? SelectedProgram
    {
        get => _selectedProgram;
        set { _selectedProgram = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string Pc { get => _pc; set { _pc = value; OnPropertyChanged(); } }
    public string A { get => _a; set { _a = value; OnPropertyChanged(); } }
    public bool Z { get => _z; set { _z = value; OnPropertyChanged(); } }
    public ulong Cycles { get => _cycles; set { _cycles = value; OnPropertyChanged(); } }
    public bool Halted { get => _halted; set { _halted = value; OnPropertyChanged(); } }
    public string? LastError { get => _lastError; set { _lastError = value; OnPropertyChanged(); } }

    public bool LedOn { get => _ledOn; set { _ledOn = value; OnPropertyChanged(); } }
    public long LedToggleCount { get => _ledToggleCount; set { _ledToggleCount = value; OnPropertyChanged(); } }
    public byte LedLastValue { get => _ledLastValue; set { _ledLastValue = value; OnPropertyChanged(); } }
    public IBrush LedColor { get => _ledColor; set { _ledColor = value; OnPropertyChanged(); } }

    public void UpdateLedColor(bool isOn, bool halted)
    {
        if (isOn)
            LedColor = OnBrush;
        else if (halted)
            LedColor = HaltedBrush;
        else
            LedColor = OffBrush;
    }

    public ICommand LoadProgramCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand StepCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand PauseCommand { get; }

    private async Task<MachineCommandResult> LoadProgramAsync()
    {
        if (SelectedProgram is null)
            return MachineCommandResult.Failure("No program selected");

        return await _session.ExecuteMachineCommandAsync("minimal-blink.load-predefined-program", SelectedProgram.Id);
    }

    private async Task<MachineCommandResult> ResetAsync()
    {
        await _session.ResetAsync();
        return MachineCommandResult.Success();
    }

    private async Task<MachineCommandResult> StepAsync()
    {
        await _session.StepInstructionAsync();
        return MachineCommandResult.Success();
    }

    private async Task<MachineCommandResult> RunAsync()
    {
        await _session.RunAsync();
        return MachineCommandResult.Success();
    }

    private async Task<MachineCommandResult> PauseAsync()
    {
        await _session.PauseAsync();
        return MachineCommandResult.Success();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

internal sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task<MachineCommandResult>> _execute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task<MachineCommandResult>> execute)
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
            var result = await _execute();
            if (!result.IsSuccess)
                System.Diagnostics.Debug.WriteLine($"Command failed: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Command threw: {ex.Message}");
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
