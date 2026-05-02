using System.Windows.Input;

namespace Symulator.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private string _statusText = "Ready";
    private string _terminalText = string.Empty;
    private string _cpuStateText = string.Empty;
    private string _selectedMachine = string.Empty;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string TerminalText
    {
        get => _terminalText;
        set => SetProperty(ref _terminalText, value);
    }

    public string CpuStateText
    {
        get => _cpuStateText;
        set => SetProperty(ref _cpuStateText, value);
    }

    public string SelectedMachine
    {
        get => _selectedMachine;
        set => SetProperty(ref _selectedMachine, value);
    }

    public List<string> Machines { get; } = ["Apple-1", "KIM-1"];

    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand StepCommand { get; }

    public MainWindowViewModel()
    {
        StartCommand = new RelayCommand(OnStart, () => true);
        PauseCommand = new RelayCommand(OnPause, () => true);
        ResetCommand = new RelayCommand(OnReset, () => true);
        StepCommand = new RelayCommand(OnStep, () => true);
    }

    private void OnStart() => StatusText = "Starting...";
    private void OnPause() => StatusText = "Paused";
    private void OnReset() => StatusText = "Reset";
    private void OnStep() => StatusText = "Step";
}
