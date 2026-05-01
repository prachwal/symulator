using System.IO;
using System.Windows.Input;
using CmosCpu.Core;
using CmosCpu.WpfApp.Commands;
using CmosCpu.WpfApp.Services;

namespace CmosCpu.WpfApp.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IUiSimulationController _controller;
    private readonly IFileDialogService _fileDialog;
    private readonly IDispatcherService _dispatcher;

    private string _statusText = "Ready";
    private int _selectedSpeedIndex = 2;
    private bool _isRunning;
    private ulong _totalCycles;
    private ushort _pc;

    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public int SelectedSpeedIndex { get => _selectedSpeedIndex; set => SetProperty(ref _selectedSpeedIndex, value); }
    public bool IsRunning { get => _isRunning; set => SetProperty(ref _isRunning, value); }
    public ulong TotalCycles { get => _totalCycles; set => SetProperty(ref _totalCycles, value); }
    public ushort PC { get => _pc; set => SetProperty(ref _pc, value); }

    public RegistersViewModel Registers { get; }
    public FlagsViewModel Flags { get; }
    public MemoryViewModel Memory { get; }
    public LedViewModel Led { get; }
    public TraceLogViewModel TraceLog { get; }
    public BusViewModel BusLog { get; }
    public DevicesViewModel Devices { get; }
    public InterruptsViewModel Interrupts { get; }

    public ICommand LoadAsmCommand { get; }
    public ICommand LoadBinCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand StepCommand { get; }
    public AsyncRelayCommand RunCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ClearTraceCommand { get; }

    public static IReadOnlyList<string> SpeedOptions { get; } = new[] { "1 Hz", "10 Hz", "100 Hz", "1 kHz", "Max" };

    public int SpeedHz
    {
        get
        {
            return SelectedSpeedIndex switch
            {
                0 => 1,
                1 => 10,
                2 => 100,
                3 => 1000,
                _ => 0,
            };
        }
    }

    public MainWindowViewModel(IUiSimulationController controller, IFileDialogService fileDialog, IDispatcherService dispatcher,
        RegistersViewModel registers, FlagsViewModel flags, MemoryViewModel memory,
        LedViewModel led, TraceLogViewModel traceLog, BusViewModel busLog,
        DevicesViewModel devices, InterruptsViewModel interrupts)
    {
        _controller = controller;
        _fileDialog = fileDialog;
        _dispatcher = dispatcher;

        Registers = registers;
        Flags = flags;
        Memory = memory;
        Led = led;
        TraceLog = traceLog;
        BusLog = busLog;
        Devices = devices;
        Interrupts = interrupts;

        LoadAsmCommand = new RelayCommand(_ => LoadAsm());
        LoadBinCommand = new RelayCommand(_ => LoadBin());
        ResetCommand = new RelayCommand(_ => Reset());
        StepCommand = new RelayCommand(_ => Step(), _ => !IsRunning);
        RunCommand = new AsyncRelayCommand(async ct => await Run(ct), () => !IsRunning);
        PauseCommand = new RelayCommand(_ => Pause(), _ => IsRunning);
        StopCommand = new RelayCommand(_ => Stop(), _ => IsRunning);
        ClearTraceCommand = new RelayCommand(_ => ClearTrace());

        _controller.SnapshotChanged += (s, snap) =>
        {
            _dispatcher.Invoke(() =>
            {
                Registers.Update(snap);
                Flags.Update(snap);
                Led.Update(snap);
                Memory.Update(snap);
                Devices.Update(snap);
                Interrupts.Update(snap);
                TotalCycles = snap.CycleCount;
                PC = snap.Registers.PC;
                StatusText = snap.Registers.Halted ? "HALTED" : "Running";
            });
        };

        _controller.TraceEntryAdded += (s, entry) =>
        {
            _dispatcher.Invoke(() => TraceLog.AddEntry(entry));
        };

        _controller.BusTransactionOccurred += (s, tx) =>
        {
            _dispatcher.Invoke(() => BusLog.AddTransaction(tx));
        };
    }

    private void LoadAsm()
    {
        string? path = _fileDialog.OpenFileDialog("Load Assembly", "Assembly files (*.asm)|*.asm|All files (*.*)|*.*");
        if (path is null) return;

        if (_controller.LoadAsm(path, out var errors))
        {
            _controller.Reset();
            StatusText = $"Loaded: {Path.GetFileName(path)}";
        }
        else
        {
            StatusText = $"Assembly errors: {string.Join("; ", errors)}";
        }
    }

    private void LoadBin()
    {
        string? path = _fileDialog.OpenFileDialog("Load Binary", "Binary files (*.bin)|*.bin|All files (*.*)|*.*");
        if (path is null) return;

        if (_controller.LoadBin(path, 0x8000, out var error))
        {
            _controller.Reset();
            StatusText = $"Loaded: {Path.GetFileName(path)}";
        }
        else
        {
            StatusText = $"Error: {error}";
        }
    }

    private void Reset()
    {
        _controller.Reset();
        TraceLog.Clear();
        StatusText = "Reset";
    }

    private void Step()
    {
        _controller.Step();
    }

    private async Task Run(CancellationToken ct)
    {
        IsRunning = true;
        _controller.SpeedHz = SpeedHz;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            await _controller.RunAsync(ct);
        }
        finally
        {
            IsRunning = false;
            CommandManager.InvalidateRequerySuggested();
            StatusText = _controller.GetSnapshot().Registers.Halted ? "HALTED" : "Paused";
        }
    }

    private void Pause()
    {
        _controller.Pause();
        StatusText = "Paused";
    }

    private void Stop()
    {
        _controller.Stop();
        TraceLog.Clear();
        StatusText = "Stopped";
    }

    private void ClearTrace()
    {
        TraceLog.Clear();
        BusLog.Clear();
    }
}
