using System.Windows.Input;
using Symulator.Application.Abstractions;

namespace Symulator.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IEmulatorController _controller;
    private readonly IMachineCatalog _catalog;
    private readonly IUiErrorService _errorService;

    private string _statusText = "Ready";
    private string _terminalText = string.Empty;
    private MachineDescriptor? _selectedMachineDescriptor;
    private bool _isRunning;
    private object? _activePanelViewModel;
    private string _selectedMachineTitle = string.Empty;

    public CpuInspectorViewModel CpuInspector { get; } = new();

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

    public string SelectedMachineTitle
    {
        get => _selectedMachineTitle;
        set => SetProperty(ref _selectedMachineTitle, value);
    }

    public bool IsMachineSelected => SelectedMachineDescriptor is not null;
    public bool HasNoMachineSelected => SelectedMachineDescriptor is null;

    public MachineDescriptor? SelectedMachineDescriptor
    {
        get => _selectedMachineDescriptor;
        set
        {
            if (SetProperty(ref _selectedMachineDescriptor, value))
            {
                OnPropertyChanged(nameof(IsMachineSelected));
                OnPropertyChanged(nameof(HasNoMachineSelected));
                if (value is not null)
                    _ = OnMachineSelected(value.Id);
            }
        }
    }

    public IReadOnlyList<MachineDescriptor> Machines => _catalog.GetMachines();

    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand StepCommand { get; }
    public ICommand ShowErrorsCommand { get; }
    public int ErrorCount => _errorService.Errors.Count;

    public MainWindowViewModel(IEmulatorController controller, IUiErrorService errorService)
    {
        _controller = controller;
        _catalog = controller.Catalog;
        _errorService = errorService;

        StartCommand = new AsyncRelayCommand(OnStart, () => !IsRunning, errorService, "Start");
        PauseCommand = new AsyncRelayCommand(OnPause, () => IsRunning, errorService, "Pause");
        ResetCommand = new AsyncRelayCommand(OnReset, () => _controller.ActiveSession is not null, errorService, "Reset");
        StepCommand = new AsyncRelayCommand(OnStep, () => _controller.ActiveSession is not null, errorService, "Step");
        ShowErrorsCommand = new RelayCommand(OnShowErrors, () => true);

        _controller.StateChanged += OnControllerStateChanged;
        _controller.StatusChanged += OnControllerStatusChanged;
        _errorService.ErrorAdded += (_, _) => OnPropertyChanged(nameof(ErrorCount));
    }

    private void OnControllerStateChanged(object? sender, EmulatorStateSnapshot snapshot)
    {
        TerminalText = snapshot.TerminalText ?? TerminalText;
        IsRunning = snapshot.IsRunning;
        CpuInspector.UpdateFromSnapshot(snapshot.Cpu);
    }

    private void OnControllerStatusChanged(object? sender, string status)
    {
        StatusText = status;
    }

    private async Task OnMachineSelected(string machineId)
    {
        try
        {
            var success = await _controller.SelectMachineAsync(machineId);
            if (!success)
            {
                StatusText = $"Cannot select machine: {machineId}";
                return;
            }

            ActivePanelViewModel = _controller.ActiveSession?.Workspace.ViewModel;
            SelectedMachineTitle = _controller.ActiveSession?.DisplayName ?? machineId;
            StatusText = $"Selected: {SelectedMachineTitle}";
            CpuInspector.UpdateFromSnapshot(_controller.Current?.Cpu);
        }
        catch (Exception ex)
        {
            _errorService.Report(ex, $"Select machine {machineId}");
        }
    }

    private async Task OnStart()
    {
        await _controller.RunAsync();
        RefreshCommandStates();
    }

    private async Task OnPause()
    {
        await _controller.PauseAsync();
        RefreshCommandStates();
    }

    private async Task OnReset()
    {
        await _controller.ResetAsync();
    }

    private async Task OnStep()
    {
        await _controller.StepInstructionAsync();
    }

    private void OnShowErrors()
    {
        var window = new Views.ErrorWindow
        {
            DataContext = new ErrorWindowViewModel(_errorService)
        };
        window.Show();
    }

    private void RefreshCommandStates()
    {
        if (StartCommand is AsyncRelayCommand ar1) ar1.RaiseCanExecuteChanged();
        if (PauseCommand is AsyncRelayCommand ar2) ar2.RaiseCanExecuteChanged();
        if (ResetCommand is AsyncRelayCommand ar3) ar3.RaiseCanExecuteChanged();
        if (StepCommand is AsyncRelayCommand ar4) ar4.RaiseCanExecuteChanged();
    }
}
