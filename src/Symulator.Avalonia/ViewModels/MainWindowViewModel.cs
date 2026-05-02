using System.Windows.Input;
using Symulator.Application.Abstractions;
using Symulator.Application.Services;

namespace Symulator.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IEmulatorController _controller;
    private readonly IMachineCatalog _catalog;

    private string _statusText = "Ready";
    private string _terminalText = string.Empty;
    private string _cpuStateText = string.Empty;
    private MachineDescriptor? _selectedMachineDescriptor;
    private bool _isRunning;
    private object? _activePanelViewModel;

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

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public object? ActivePanelViewModel
    {
        get => _activePanelViewModel;
        set => SetProperty(ref _activePanelViewModel, value);
    }

    public MachineDescriptor? SelectedMachineDescriptor
    {
        get => _selectedMachineDescriptor;
        set
        {
            if (SetProperty(ref _selectedMachineDescriptor, value) && value is not null)
            {
                _ = OnMachineSelected(value.Id);
            }
        }
    }

    public IReadOnlyList<MachineDescriptor> Machines => _catalog.GetMachines();

    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand StepCommand { get; }

    public MainWindowViewModel(IEmulatorController controller)
    {
        _controller = controller;
        _catalog = controller.Catalog;

        StartCommand = new RelayCommand(async () => await OnStart(), () => !IsRunning);
        PauseCommand = new RelayCommand(async () => await OnPause(), () => IsRunning);
        ResetCommand = new RelayCommand(async () => await OnReset(), () => _controller.ActiveSession is not null);
        StepCommand = new RelayCommand(async () => await OnStep(), () => _controller.ActiveSession is not null);

        _controller.StateChanged += OnControllerStateChanged;
        _controller.StatusChanged += OnControllerStatusChanged;
    }

    private void OnControllerStateChanged(object? sender, EmulatorStateSnapshot snapshot)
    {
        TerminalText = snapshot.TerminalText ?? TerminalText;
        IsRunning = snapshot.IsRunning;

        if (snapshot.Cpu is not null)
        {
            var cpu = snapshot.Cpu;
            CpuStateText = $"PC={cpu.Pc}  A={cpu.A}  X={cpu.X}  Y={cpu.Y}  SP={cpu.Sp}\n" +
                           $"Flags: {cpu.Flags}  Cycles: {cpu.CycleCount}  Halted: {cpu.IsHalted}";
        }
    }

    private void OnControllerStatusChanged(object? sender, string status)
    {
        StatusText = status;
    }

    private async Task OnMachineSelected(string machineId)
    {
        bool success = await _controller.SelectMachineAsync(machineId);
        if (success)
        {
            StatusText = $"Selected: {machineId}";
            var desc = Machines.FirstOrDefault(m => m.Id == machineId);
            SelectedMachineDescriptor = desc;

            var panel = _controller.ActiveSession?.Panels.FirstOrDefault();
            ActivePanelViewModel = panel?.ViewModel;
        }
        else
        {
            StatusText = $"Failed to select: {machineId}";
        }
    }

    private async Task OnStart()
    {
        await _controller.RunAsync();
        ((RelayCommand)StartCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PauseCommand).RaiseCanExecuteChanged();
    }

    private async Task OnPause()
    {
        await _controller.PauseAsync();
        ((RelayCommand)StartCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PauseCommand).RaiseCanExecuteChanged();
    }

    private async Task OnReset()
    {
        await _controller.ResetAsync();
    }

    private async Task OnStep()
    {
        await _controller.StepInstructionAsync();
    }
}
