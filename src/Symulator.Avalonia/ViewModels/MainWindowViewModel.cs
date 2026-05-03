using System.Diagnostics;
using System.Windows.Input;
using Avalonia.Threading;
using Symulator.Application.Abstractions;

namespace Symulator.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IEmulatorController _controller;
    private readonly IMachineCatalog _catalog;
    private readonly IUiErrorService _errorService;

    private string _statusText = "Ready";
    private MachineDescriptor? _selectedMachineDescriptor;
    private bool _isRunning;
    private bool _isMachineInitialized;
    private object? _activePanelViewModel;
    private string _selectedMachineTitle = "Select machine";
    private string _lastErrorText = "-";
    private readonly string _logPath;

    public CpuInspectorViewModel CpuInspector { get; } = new();

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    public bool IsMachineInitialized
    {
        get => _isMachineInitialized;
        set => SetProperty(ref _isMachineInitialized, value);
    }

    public bool CanStart => IsMachineInitialized && !IsRunning;
    public bool CanPause => IsRunning;
    public bool CanStep => IsMachineInitialized && !IsRunning;
    public bool CanReset => IsMachineSelected;

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

    public string LastErrorText
    {
        get => _lastErrorText;
        set => SetProperty(ref _lastErrorText, value);
    }

    public string LogPath => _logPath;

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
                OnPropertyChanged(nameof(CanReset));
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
    public ICommand OpenLogsCommand { get; }
    public int ErrorCount => _errorService.Errors.Count;

    public MainWindowViewModel(IEmulatorController controller, IUiErrorService errorService)
    {
        _controller = controller;
        _catalog = controller.Catalog;
        _errorService = errorService;
        _logPath = Path.Combine(AppContext.BaseDirectory, "logs");

        StartCommand = new AsyncRelayCommand(OnStart, () => CanStart, errorService, "Start");
        PauseCommand = new AsyncRelayCommand(OnPause, () => CanPause, errorService, "Pause");
        ResetCommand = new AsyncRelayCommand(OnReset, () => CanReset, errorService, "Reset");
        StepCommand = new AsyncRelayCommand(OnStep, () => CanStep, errorService, "Step");
        ShowErrorsCommand = new RelayCommand(OnShowErrors, () => true);
        OpenLogsCommand = new RelayCommand(OpenLogsFolder, () => true);

        _controller.StateChanged += OnControllerStateChanged;
        _controller.StatusChanged += OnControllerStatusChanged;
        _errorService.ErrorAdded += OnErrorAdded;

        CpuInspector.SetUnavailable();
    }

    private void OnControllerStateChanged(object? sender, EmulatorStateSnapshot snapshot)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsRunning = snapshot.IsRunning;
            IsMachineInitialized = snapshot.Cpu is not null;
            CpuInspector.UpdateFromSnapshot(snapshot.Cpu);
            CpuInspector.SetMachineInfo(SelectedMachineTitle, IsRunning ? "Running" : (IsMachineInitialized ? "Initialized" : "Selected"), CpuInspector.BootMode);
            RefreshDerivedStates();
            RefreshCommandStates();
        });
    }

    private void OnControllerStatusChanged(object? sender, string status)
    {
        Dispatcher.UIThread.Post(() => StatusText = status);
    }

    private void OnErrorAdded(object? sender, UiErrorEntry entry)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LastErrorText = entry.Message;
            CpuInspector.SetLastError(entry.Message);
            OnPropertyChanged(nameof(ErrorCount));
        });
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
            IsMachineInitialized = _controller.Current?.Cpu is not null;
            StatusText = $"Selected: {SelectedMachineTitle}";
            CpuInspector.SetMachineInfo(SelectedMachineTitle, "Selected", "Not booted");
            CpuInspector.UpdateFromSnapshot(_controller.Current?.Cpu);
            RefreshDerivedStates();
            RefreshCommandStates();
        }
        catch (Exception ex)
        {
            _errorService.Report(ex, $"Select machine {machineId}");
        }
    }

    private async Task OnStart()
    {
        if (!IsMachineInitialized)
        {
            _errorService.Report("Machine is not initialized. Use Boot MON, Boot BASIC or Reset first.");
            return;
        }

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
        RefreshCommandStates();
    }

    private async Task OnStep()
    {
        if (!IsMachineInitialized)
        {
            _errorService.Report("Machine is not initialized. Use Boot MON, Boot BASIC or Reset first.");
            return;
        }

        await _controller.StepInstructionAsync();
        RefreshCommandStates();
    }

    private void OnShowErrors()
    {
        var window = new Views.ErrorWindow
        {
            DataContext = new ErrorWindowViewModel(_errorService, _logPath)
        };
        window.Show();
    }

    private void OpenLogsFolder()
    {
        Directory.CreateDirectory(_logPath);
        var psi = new ProcessStartInfo
        {
            FileName = _logPath,
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    private void RefreshDerivedStates()
    {
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(CanPause));
        OnPropertyChanged(nameof(CanStep));
        OnPropertyChanged(nameof(CanReset));
    }

    private void RefreshCommandStates()
    {
        if (StartCommand is AsyncRelayCommand ar1) ar1.RaiseCanExecuteChanged();
        if (PauseCommand is AsyncRelayCommand ar2) ar2.RaiseCanExecuteChanged();
        if (ResetCommand is AsyncRelayCommand ar3) ar3.RaiseCanExecuteChanged();
        if (StepCommand is AsyncRelayCommand ar4) ar4.RaiseCanExecuteChanged();
        if (OpenLogsCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
    }
}
