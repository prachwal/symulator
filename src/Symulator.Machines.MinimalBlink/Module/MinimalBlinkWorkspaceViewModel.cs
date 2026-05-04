using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Media;
using Symulator.Application.Abstractions;
using Symulator.Application.Assembly;
using Symulator.Application.Solutions;

namespace Symulator.Machines.MinimalBlink.Module;

public sealed class MinimalBlinkWorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IMachineSession _session;
    private readonly AssemblyProgramCatalog _catalog = new();
    private readonly SolutionDefinitionLoader _solutionLoader = new();

    private static string? FindAsmDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "programs", "asm");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        return null;
    }

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
    private IBrush _ledColor = OffBrush;
    private bool[,]? _lcdPixels;
    private List<string>? _terminalLines;
    private string _terminalInputText = string.Empty;
    private string _asmSourceText = string.Empty;
    private string _programStatus = "Not compiled";
    private string _selectedSolutionSummary = "No solution selected";
    private List<AssemblyListingLine>? _listingLines;
    private int? _currentListingLineIndex;
    private AssemblySourceProgram? _selectedAsmProgram;
    private SolutionDefinition? _selectedSolution;
    private List<AssemblySourceProgram> _asmPrograms = new();
    private bool _showCpuModule = true;
    private bool _showLedModule;
    private bool _showLcdModule;
    private bool _showUartModule;
    private bool _showI2cModule;
    private bool _showRtcModule;
    private bool _isSolutionLoading;
    private int _solutionLoadVersion;
    private string _rtcCurrentTime = string.Empty;
    private string _rtcTimeMode = string.Empty;
    private string _rtcI2cAddress = string.Empty;
    private string _rtcBusAddress = string.Empty;
    private IReadOnlyList<CpuRegisterSnapshot> _rtcRegisters = [];

    private static readonly IBrush OnBrush = new SolidColorBrush(Color.Parse("#4DFF88"));
    private static readonly IBrush OffBrush = new SolidColorBrush(Color.Parse("#1a3a2a"));
    private static readonly IBrush HaltedBrush = new SolidColorBrush(Color.Parse("#FF5C5C"));

    public MinimalBlinkWorkspaceViewModel(IMachineSession session)
    {
        _session = session;
        ResetCommand = new AsyncRelayCommand(ResetAsync);
        StepCommand = new AsyncRelayCommand(StepAsync);
        RunCommand = new AsyncRelayCommand(RunAsync);
        PauseCommand = new AsyncRelayCommand(PauseAsync);
        CompileCommand = new AsyncRelayCommand(CompileAsync, () => !IsSolutionLoading);
        LoadAndResetCommand = new AsyncRelayCommand(LoadAndResetAsync, () => !IsSolutionLoading);

        _ = InitializeAsmProgramsAsync();
    }

    public bool IsSolutionLoading
    {
        get => _isSolutionLoading;
        private set
        {
            if (_isSolutionLoading == value)
                return;

            _isSolutionLoading = value;
            OnPropertyChanged();
            RaiseCommandsCanExecuteChanged();
        }
    }

    private void RaiseCommandsCanExecuteChanged()
    {
        if (CompileCommand is AsyncRelayCommand c1) c1.RaiseCanExecuteChanged();
        if (LoadAndResetCommand is AsyncRelayCommand c2) c2.RaiseCanExecuteChanged();
    }

    private async Task InitializeAsmProgramsAsync()
    {
        var asmDir = FindAsmDirectory();
        if (asmDir is null)
        {
            AsmPrograms = new List<AssemblySourceProgram>();
            return;
        }

        try
        {
            var programs = await _catalog.LoadProgramsAsync(asmDir);
            AsmPrograms = programs.ToList();
            if (programs.Count > 0)
            {
                SelectedAsmProgram = programs[0];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load ASM programs: {ex.Message}");
            AsmPrograms = new List<AssemblySourceProgram>();
        }
    }

    public bool[,]? LcdPixels
    {
        get => _lcdPixels;
        set { _lcdPixels = value; OnPropertyChanged(); }
    }

    public List<string>? TerminalLines
    {
        get => _terminalLines;
        set { _terminalLines = value; OnPropertyChanged(); }
    }

    public string TerminalInputText
    {
        get => _terminalInputText;
        set { _terminalInputText = value; OnPropertyChanged(); }
    }

    public string AsmSourceText
    {
        get => _asmSourceText;
        set { _asmSourceText = value; OnPropertyChanged(); }
    }

    public string ProgramStatus
    {
        get => _programStatus;
        set { _programStatus = value; OnPropertyChanged(); }
    }

    public string SelectedSolutionSummary
    {
        get => _selectedSolutionSummary;
        set { _selectedSolutionSummary = value; OnPropertyChanged(); }
    }

    public List<AssemblyListingLine>? ListingLines
    {
        get => _listingLines;
        set { _listingLines = value; OnPropertyChanged(); UpdateExecutableListing(); }
    }

    private void UpdateExecutableListing()
    {
        ExecutableListingLines = _listingLines?
            .Where(x => x.Bytes is { Length: > 0 })
            .ToList();
        OnPropertyChanged(nameof(ExecutableListingLines));
    }

    public List<AssemblyListingLine>? ExecutableListingLines { get; private set; }

    public int? CurrentListingLineIndex
    {
        get => _currentListingLineIndex;
        set { _currentListingLineIndex = value; OnPropertyChanged(); }
    }

    public bool ShowCpuModule
    {
        get => _showCpuModule;
        set { _showCpuModule = value; OnPropertyChanged(); }
    }

    public bool ShowLedModule
    {
        get => _showLedModule;
        set { _showLedModule = value; OnPropertyChanged(); }
    }

    public bool ShowLcdModule
    {
        get => _showLcdModule;
        set { _showLcdModule = value; OnPropertyChanged(); }
    }

    public bool ShowUartModule
    {
        get => _showUartModule;
        set
        {
            _showUartModule = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowNoCenterModule));
        }
    }

    public bool ShowI2cModule
    {
        get => _showI2cModule;
        set
        {
            _showI2cModule = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowNoCenterModule));
        }
    }

    public bool ShowNoCenterModule => !ShowUartModule && !ShowI2cModule;

    public bool ShowRtcModule
    {
        get => _showRtcModule;
        set { _showRtcModule = value; OnPropertyChanged(); }
    }

    public string RtcCurrentTime
    {
        get => _rtcCurrentTime;
        set { _rtcCurrentTime = value; OnPropertyChanged(); }
    }

    public string RtcTimeMode
    {
        get => _rtcTimeMode;
        set { _rtcTimeMode = value; OnPropertyChanged(); }
    }

    public string RtcI2cAddress
    {
        get => _rtcI2cAddress;
        set { _rtcI2cAddress = value; OnPropertyChanged(); }
    }

    public string RtcBusAddress
    {
        get => _rtcBusAddress;
        set { _rtcBusAddress = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<CpuRegisterSnapshot> RtcRegisters
    {
        get => _rtcRegisters;
        set { _rtcRegisters = value; OnPropertyChanged(); }
    }

    public void UpdateCurrentInstructionFromPc(ushort pc)
    {
        var lines = ExecutableListingLines;
        if (lines is null || lines.Count == 0)
        {
            CurrentListingLineIndex = null;
            return;
        }

        foreach (var line in lines)
            line.CurrentLineMarker = string.Empty;

        var index = lines.FindIndex(x =>
            x.Address.HasValue &&
            x.Address.Value == pc &&
            x.Bytes is { Length: > 0 });

        if (index >= 0)
        {
            lines[index].CurrentLineMarker = "▶";
            CurrentListingLineIndex = index;
        }
        else
        {
            CurrentListingLineIndex = null;
        }

        OnPropertyChanged(nameof(ExecutableListingLines));
    }

    public List<AssemblySourceProgram> AsmPrograms
    {
        get => _asmPrograms;
        set { _asmPrograms = value; OnPropertyChanged(); }
    }

    public AssemblySourceProgram? SelectedAsmProgram
    {
        get => _selectedAsmProgram;
        set
        {
            if (ReferenceEquals(_selectedAsmProgram, value))
                return;

            _selectedAsmProgram = value;
            OnPropertyChanged();

            if (value is null)
            {
                System.Threading.Interlocked.Increment(ref _solutionLoadVersion);
                IsSolutionLoading = false;
                AsmSourceText = string.Empty;
                ProgramStatus = "No ASM program selected";
                SelectedSolutionSummary = "No solution selected";
                ListingLines = null;
                CurrentListingLineIndex = null;
                ApplySolutionToVisibleModules(null);
                return;
            }

            AsmSourceText = value.SourceText;
            ProgramStatus = $"Selected, not compiled: {value.Id}";
            ListingLines = null;
            CurrentListingLineIndex = null;
            IsSolutionLoading = true;

            var loadVersion = System.Threading.Interlocked.Increment(ref _solutionLoadVersion);
            _ = LoadSelectedSolutionAsync(value, loadVersion);
        }
    }

    private async Task LoadSelectedSolutionAsync(AssemblySourceProgram program, int loadVersion)
    {
        try
        {
            var solution = await _solutionLoader.LoadForSourceAsync(program.FilePath);
            if (loadVersion != _solutionLoadVersion)
                return;

            _selectedSolution = solution;
            SelectedSolutionSummary = $"Solution: {_selectedSolution.Name}";
            ApplySolutionToVisibleModules(_selectedSolution);

            if (_session is MinimalBlinkMachineSession asmSession)
                asmSession.SetSelectedSolution(_selectedSolution);
        }
        catch (Exception ex)
        {
            if (loadVersion != _solutionLoadVersion)
                return;

            _selectedSolution = SolutionDefinition.CreateFallback(program.Id, Path.GetFileName(program.FilePath));
            SelectedSolutionSummary = $"Solution fallback: {program.Id}";
            ApplySolutionToVisibleModules(_selectedSolution);

            if (_session is MinimalBlinkMachineSession asmSession)
                asmSession.SetSelectedSolution(_selectedSolution);

            System.Diagnostics.Debug.WriteLine($"Failed to load solution manifest: {ex.Message}");
        }
        finally
        {
            if (loadVersion == _solutionLoadVersion)
                IsSolutionLoading = false;
        }
    }

    private void ApplySolutionToVisibleModules(SolutionDefinition? solution)
    {
        if (solution is null)
        {
            ShowCpuModule = true;
            ShowLedModule = false;
            ShowLcdModule = false;
            ShowUartModule = false;
            ShowI2cModule = false;
            ShowRtcModule = false;
            return;
        }

        var visibleModuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in solution.Ui.Layout.Left) visibleModuleIds.Add(id);
        foreach (var id in solution.Ui.Layout.Center) visibleModuleIds.Add(id);
        foreach (var id in solution.Ui.Layout.Right) visibleModuleIds.Add(id);
        foreach (var id in solution.Ui.Layout.Bottom) visibleModuleIds.Add(id);

        bool HasDeviceType(string type) => solution.Devices.Any(d =>
            d.Visible &&
            visibleModuleIds.Contains(d.Id) &&
            string.Equals(d.Type, type, StringComparison.OrdinalIgnoreCase));

        ShowCpuModule = visibleModuleIds.Contains("cpu") || HasDeviceType("cpu");
        ShowLedModule = HasDeviceType("led-mmio");
        ShowLcdModule = HasDeviceType("hd44780-mmio") || HasDeviceType("hd44780-pcf8574");
        ShowUartModule = HasDeviceType("uart-mmio");
        ShowI2cModule = HasDeviceType("i2c-controller-mmio");
        ShowRtcModule = HasDeviceType("rtc-i2c") || HasDeviceType("rtc-mmio");
    }

    public async Task SendTerminalInputAsync()
    {
        var text = TerminalInputText;
        TerminalInputText = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return;

        var currentLines = _terminalLines ?? new List<string>();
        _terminalLines = new List<string>(currentLines) { $"> {text}" };
        OnPropertyChanged(nameof(TerminalLines));

        await _session.SendInputAsync(text + "\n");
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

    public ICommand ResetCommand { get; }
    public ICommand StepCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand CompileCommand { get; }
    public ICommand LoadAndResetCommand { get; }

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

    private async Task<MachineCommandResult> CompileAsync()
    {
        if (IsSolutionLoading)
            return MachineCommandResult.Failure("Solution is still loading, please wait.");

        var asmSession = _session as MinimalBlinkMachineSession;
        if (asmSession is null || SelectedAsmProgram is null)
            return MachineCommandResult.Failure("No ASM program selected");

        var result = asmSession.CompileFromSource(SelectedAsmProgram.Id, AsmSourceText);

        if (!result.Success)
        {
            var errors = string.Join("; ",
                result.Diagnostics.Where(d => d.Severity == "Error").Select(d => d.Message));
            var message = $"Compile failed: {errors}";
            ProgramStatus = message[..Math.Min(120, message.Length)];
            ListingLines = null;
            CurrentListingLineIndex = null;
            return MachineCommandResult.Failure(errors);
        }

        ProgramStatus = $"Compiled: {SelectedAsmProgram.Id}, {result.Image!.Bytes.Length} bytes at ${result.Image.LoadAddress:X4}";
        ListingLines = result.Image.Listing.ToList();
        CurrentListingLineIndex = null;
        return MachineCommandResult.Success();
    }

    private async Task<MachineCommandResult> LoadAndResetAsync()
    {
        if (IsSolutionLoading)
            return MachineCommandResult.Failure("Solution is still loading, please wait.");

        var asmSession = _session as MinimalBlinkMachineSession;
        if (asmSession is null)
            return MachineCommandResult.Failure("Not a MinimalBlink session");

        asmSession.LoadAndResetCompiled();

        if (asmSession.GetCompiledImage() is { } img)
        {
            ProgramStatus = $"Loaded: {img.ProgramId}, PC=${img.StartAddress:X4}";
        }
        else
        {
            ProgramStatus = "Loaded";
        }
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
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task<MachineCommandResult>> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting) return false;
        if (_canExecute is not null && !_canExecute()) return false;
        return true;
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

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
