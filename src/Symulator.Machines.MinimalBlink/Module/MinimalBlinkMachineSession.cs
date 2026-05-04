using CmosCpu.Computer.Devices;
using CmosCpu.Computer.Devices.I2c;
using CmosCpu.Computer.Devices.Rtc;
using CmosCpu.Computer.Devices.Serial;
using NLog;
using Symulator.Application.Abstractions;
using Symulator.Application.Assembly;
using Symulator.Application.Solutions;
using Symulator.Application.Terminal;
using Symulator.Machines.MinimalBlink.Cpu;
using Symulator.Machines.MinimalBlink.Hardware;
using Symulator.Machines.MinimalBlink.Memory;
using Symulator.Machines.MinimalBlink.Programs;
using Symulator.Machines.MinimalBlink.Models;

namespace Symulator.Machines.MinimalBlink.Module;

public sealed class MinimalBlinkMachineSession : IMachineSession
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private MinimalBlinkCpu? _cpu;
    private MinimalBlinkMemory? _memory;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private MinimalBlinkWorkspaceViewModel? _workspaceVm;
    private bool _isRunning;
    private bool _isInitialized;
    private string _status = "Ready";
    private readonly IUiDispatcher? _uiDispatcher;
    private Hd44780Lcd? _lcd;
    private Hd44780DirectBusAdapter? _lcdBus;
    private I2cBus? _i2cBus;
    private MinimalBlinkLcdBuffer? _lcdBuffer;
    private MemoryMappedI2cController? _i2cController;
    private UartDevice? _uart;
    private MemoryMappedUartAdapter? _uartAdapter;
    private TerminalBuffer? _terminal;
    private RtcClockCore? _rtcClock;
    private RtcI2cDevice? _rtcI2c;
    private RtcBusMappedDevice? _rtcBus;
    private string? _selectedProgramId;
    private string? _loadedProgramId;
    private AssemblyProgramImage? _loadedAssemblyImage;
    private string? _compiledProgramId;
    private AssemblyProgramImage? _compiledImage;
    private string? _asmSourceText;
    private SolutionDefinition? _selectedSolution;
    private SolutionDefinition? _loadedSolution;
    private readonly MinimalBlinkHardwareSolutionBuilder _hardwareBuilder = new();

    private const int _instructionsPerBatch = 100;
    private const int _uiRefreshDelayMs = 16;

    public MinimalBlinkMachineSession(IUiDispatcher? uiDispatcher = null)
    {
        _uiDispatcher = uiDispatcher;
    }

    public string MachineId => "minimal-blink";
    public string DisplayName => "Minimal Blink Computer";
    public EmulatorStateSnapshot Current => BuildSnapshot();
    public IMachineWorkspaceDescriptor Workspace => GetOrCreateWorkspace();

    public event EventHandler<EmulatorStateSnapshot>? StateChanged;
    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? StatusChanged;

    private IMachineWorkspaceDescriptor GetOrCreateWorkspace()
    {
        _workspaceVm ??= new MinimalBlinkWorkspaceViewModel(this);
        return new MachineWorkspaceDescriptor("minimal-blink", "Minimal Blink Computer", _workspaceVm);
    }

    private EmulatorStateSnapshot BuildSnapshot()
    {
        if (_cpu is null)
            return new EmulatorStateSnapshot("minimal-blink", false, false, null, string.Empty, 0);

        var registers = new List<CpuRegisterSnapshot>
        {
            new("Z", _cpu.Z ? "1" : "0"),
            new("Cycles", _cpu.CycleCount.ToString()),
        };

        var cpuSnap = new CpuStateSnapshot(
            $"${_cpu.PC:X4}",
            $"${_cpu.A:X2}",
            "--",
            "--",
            "--",
            _cpu.Halted ? "HLT" : "RUN",
            _cpu.CycleCount,
            _cpu.Halted,
            registers);

        return new EmulatorStateSnapshot("minimal-blink", _isRunning, _cpu.Halted, cpuSnap, string.Empty, (long)_cpu.CycleCount,
            Rtc: _rtcClock?.CreateSnapshot(
                i2cAddress: _rtcI2c?.Address,
                directBusBaseAddress: _rtcBus?.BaseAddress,
                directBusMode: _rtcBus?.Mode));
    }

    private void Initialize()
    {
        if (_isInitialized)
            return;

        // Minimal fallback — real hardware is built from solution
        _memory = new MinimalBlinkMemory();
        _cpu = new MinimalBlinkCpu(_memory);
        _isInitialized = true;
        Logger.Info("Minimal Blink initialized (minimal fallback)");
    }

    public void NotifyAsmProgramsLoaded(IReadOnlyList<AssemblySourceProgram> programs)
    {
        // No-op: selection is handled by the ViewModel.
    }

    public void SetSelectedSolution(SolutionDefinition solution)
    {
        _selectedSolution = solution;
    }

    private void RebuildFromSolution(SolutionDefinition solution)
    {
        var rt = _hardwareBuilder.Build(solution);
        _memory = rt.Memory;
        _cpu = rt.Cpu;
        _lcd = rt.Lcd;
        _lcdBuffer = rt.LcdBuffer;
        _lcdBus = rt.LcdBus;
        _i2cBus = rt.I2cBus;
        _i2cController = rt.I2cController;
        _uart = rt.Uart;
        _uartAdapter = rt.UartAdapter;
        _terminal = rt.Terminal;
        _rtcClock = rt.RtcClock;
        _rtcI2c = rt.RtcI2c;
        _rtcBus = rt.RtcBus;
        _isInitialized = true;
        Logger.Info("Minimal Blink hardware rebuilt from solution: {Id}", solution.Id);
    }

    private void TickDevices()
    {
        _lcd?.Tick(_cpu?.CycleCount ?? 0);
    }

    public AssemblyProgramImage? GetCompiledImage() => _compiledImage;

    public AssemblyResult CompileFromSource(string programId, string sourceText)
    {
        var assembler = new PseudoCpuAssembler();
        var result = assembler.Assemble(programId, sourceText);
        if (result.Success && result.Image is not null)
        {
            _compiledProgramId = programId;
            _compiledImage = result.Image;
            _asmSourceText = sourceText;
            _status = $"Compiled: {programId} ({result.Image.Bytes.Length} bytes)";
            Logger.Info("Minimal Blink compiled: {Id} at ${Addr:X4} ({Count} bytes)",
                programId, result.Image.LoadAddress, result.Image.Bytes.Length);
        }
        else
        {
            _compiledProgramId = null;
            _compiledImage = null;
            _status = "Compile failed";
            Logger.Warn("Minimal Blink compile failed for {Id}: {Errors}",
                programId, string.Join("; ", result.Diagnostics.Where(d => d.Severity == "Error").Select(d => d.Message)));
        }
        PublishSnapshot();
        return result;
    }

    public void LoadAndResetCompiled()
    {
        if (_compiledImage is null)
        {
            _status = "No compiled program. Use Compile first.";
            PublishSnapshot();
            return;
        }

        var solution = _selectedSolution
            ?? SolutionDefinition.CreateFallback(_compiledImage.ProgramId, _compiledImage.ProgramId + ".asm");

        RebuildFromSolution(solution);

        _memory.ClearAll();
        _memory.Load(_compiledImage.LoadAddress, _compiledImage.Bytes);

        _cpu.Reset();
        _cpu.PC = _compiledImage.StartAddress;

        _loadedSolution = solution;
        _loadedAssemblyImage = new AssemblyProgramImage
        {
            ProgramId = _compiledImage.ProgramId,
            LoadAddress = _compiledImage.LoadAddress,
            StartAddress = _compiledImage.StartAddress,
            Bytes = _compiledImage.Bytes.ToArray(),
            HexDump = _compiledImage.HexDump,
            Listing = _compiledImage.Listing.ToArray()
        };
        _loadedProgramId = _compiledImage.ProgramId;

        _isRunning = false;
        _status = $"Loaded: {_compiledImage.ProgramId}, PC=${_cpu.PC:X4}";
        Logger.Info("Minimal Blink load & reset: {Id} at ${Addr:X4} (PC=${PC:X4})",
            _compiledImage.ProgramId, _compiledImage.LoadAddress, _cpu.PC);
        PublishSnapshot();
    }


    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (_loadedAssemblyImage is not null && _loadedSolution is not null)
        {
            RebuildFromSolution(_loadedSolution);
            _memory.ClearAll();
            _memory.Load(_loadedAssemblyImage.LoadAddress, _loadedAssemblyImage.Bytes);
            _cpu.Reset();
            _cpu.PC = _loadedAssemblyImage.StartAddress;
            _isRunning = false;
            _status = $"Reset: {_loadedAssemblyImage.ProgramId}, PC=${_cpu.PC:X4}";
            PublishSnapshot();
            Logger.Info("Minimal Blink reset: {Id} from loaded solution", _loadedAssemblyImage.ProgramId);
            return Task.CompletedTask;
        }

        // Legacy predefined program fallback
        if (_loadedProgramId is not null)
        {
            Initialize();
            var program = MinimalBlinkPredefinedPrograms.FindById(_loadedProgramId);
            if (program is not null)
            {
                _memory!.ClearAll();
                _memory.Load(program.LoadAddress, program.Bytes);
                _cpu!.Reset();
                _cpu.PC = program.StartAddress;
                _isRunning = false;
                _status = $"Reset: {program.Name}, PC=${_cpu.PC:X4}";
                PublishSnapshot();
                Logger.Info("Minimal Blink reset: {Name} (legacy)", program.Name);
                return Task.CompletedTask;
            }
        }

        _status = "Reset: no loaded program";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    public Task StepInstructionAsync(CancellationToken cancellationToken = default)
    {
        if (_cpu is null || _memory is null || (_loadedAssemblyImage is null && _loadedProgramId is null))
        {
            _status = "No program loaded. Use Compile and Load & Reset.";
            PublishSnapshot();
            return Task.CompletedTask;
        }

        if (_cpu.PC == 0)
        {
            _status = "No executable PC. Use Load & Reset.";
            PublishSnapshot();
            return Task.CompletedTask;
        }

        _cpu.Step();
        TickDevices();
        _status = _cpu.Halted ? "Halted" : "Stepped";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        Initialize();

        if (_runTask is { IsCompleted: false })
            return Task.CompletedTask;

        if (_cpu is null || _memory is null || (_loadedAssemblyImage is null && _loadedProgramId is null))
        {
            _status = "No program loaded. Use Compile and Load & Reset.";
            PublishSnapshot();
            return Task.CompletedTask;
        }

        if (_cpu.Halted)
        {
            _isRunning = false;
            _status = "Halted";
            PublishSnapshot();
            Logger.Info("Minimal Blink Computer run not started because CPU is already halted");
            return Task.CompletedTask;
        }

        _runCts?.Dispose();
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var runToken = _runCts.Token;

        _isRunning = true;
        _status = "Running";
        PublishSnapshot();
        Logger.Info("Minimal Blink Computer run started");

        _runTask = RunLoopAsync(runToken);
        return Task.CompletedTask;
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _cpu is not null && !_cpu.Halted)
            {
                for (int i = 0; i < _instructionsPerBatch; i++)
                {
                    if (cancellationToken.IsCancellationRequested || _cpu is null || _cpu.Halted)
                        break;

                    _cpu.Step();
                    TickDevices();
                }

                PublishSnapshot();
                await Task.Delay(_uiRefreshDelayMs, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.Error(ex, "Minimal Blink Computer run loop error");
            _status = $"Error: {ex.Message}";
        }
        finally
        {
            _isRunning = false;
            _status = _cpu?.Halted == true ? "Halted" : "Paused";
            PublishSnapshot();
            Logger.Info("Minimal Blink Computer run stopped");
        }
    }

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        var cts = _runCts;
        var task = _runTask;

        if (cts is not null)
            await cts.CancelAsync();

        if (task is not null)
        {
            try { await task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken); }
            catch (TimeoutException) { Logger.Warn("Minimal Blink run task did not stop within 2s"); }
            _runTask = null;
        }

        if (cts is not null)
        {
            cts.Dispose();
            if (ReferenceEquals(_runCts, cts))
                _runCts = null;
        }

        _isRunning = false;
        _status = "Paused";
        PublishSnapshot();
        Logger.Info("Minimal Blink Computer paused");
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_uart is null)
            return Task.CompletedTask;

        foreach (char ch in text)
        {
            switch (ch)
            {
                case '\r': _uart.ReceiveFromTerminal(0x0D); break;
                case '\n': _uart.ReceiveFromTerminal(0x0A); break;
                default:   _uart.ReceiveFromTerminal((byte)ch); break;
            }
        }
        return Task.CompletedTask;
    }

    public async Task<MachineCommandResult> ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default)
    {
        switch (commandId)
        {
            case "minimal-blink.reset":
                await ResetAsync(cancellationToken);
                return MachineCommandResult.Success();

            case "minimal-blink.load-predefined-program":
                if (parameter is string programId)
                {
                    Initialize();
                    var program = MinimalBlinkPredefinedPrograms.FindById(programId);
                    if (program is null)
                        return MachineCommandResult.Failure($"Unknown program: {programId}");

                    var loadAddress = program.LoadAddress;
                    var romEnd = MinimalBlinkMemory.RomEnd;
                    var lastByteAddr = (ushort)(loadAddress + program.Bytes.Length - 1);
                    if (lastByteAddr > romEnd || loadAddress < MinimalBlinkMemory.RomStart)
                    {
                        var msg = $"Program '{program.Name}' load range ${loadAddress:X4}-${lastByteAddr:X4} exceeds ROM (${MinimalBlinkMemory.RomStart:X4}-${romEnd:X4})";
                        Logger.Warn(msg);
                        return MachineCommandResult.Failure(msg);
                    }

                    _memory!.ClearAll();
                    _memory.Load(loadAddress, program.Bytes);
                    _cpu!.Reset();
                    _cpu.PC = program.StartAddress;
                    _loadedProgramId = program.Id;
                    _selectedProgramId = program.Id;
                    _loadedAssemblyImage = null;
                    _status = $"Loaded: {program.Name}, PC=${_cpu.PC:X4}";
                    Logger.Info("Minimal Blink loaded program: {Name} id={Id} at ${Start:X4} ({ByteCount} bytes)",
                        program.Name, program.Id, program.StartAddress, program.Bytes.Length);
                    PublishSnapshot();
                    return MachineCommandResult.Success();
                }
                return MachineCommandResult.Failure("Program ID required");

            case "minimal-blink.load-blink":
                return await ExecuteMachineCommandAsync("minimal-blink.load-predefined-program", "blink-led", cancellationToken);

            case "minimal-blink.step":
                await StepInstructionAsync(cancellationToken);
                return MachineCommandResult.Success();

            case "minimal-blink.clear":
                _memory?.ClearAll();
                _cpu?.Reset();
                _loadedProgramId = null;
                _loadedAssemblyImage = null;
                _status = "Cleared";
                PublishSnapshot();
                return MachineCommandResult.Success();

            case "minimal-blink.is-running":
                return MachineCommandResult.Success();

            default:
                Logger.Warn("Unknown command: {CommandId}", commandId);
                return MachineCommandResult.Failure($"Unknown command: {commandId}");
        }
    }

    private void PublishSnapshot()
    {
        var snapshot = BuildSnapshot();
        StateChanged?.Invoke(this, snapshot);

        if (_workspaceVm is null)
            return;

        Action update = () =>
        {
            _workspaceVm.StatusText = _status;
            _workspaceVm.Pc = $"${_cpu?.PC:X4}" ?? "$0000";
            _workspaceVm.UpdateCurrentInstructionFromPc(_cpu?.PC ?? 0);
            _workspaceVm.A = $"${_cpu?.A:X2}" ?? "$00";
            _workspaceVm.Z = _cpu?.Z ?? false;
            _workspaceVm.Cycles = _cpu?.CycleCount ?? 0;
            _workspaceVm.Halted = _cpu?.Halted ?? false;
            _workspaceVm.LastError = _cpu?.LastError;

            if (_memory is not null)
            {
                bool prevLedOn = _workspaceVm.LedOn;
                _workspaceVm.LedOn = _memory.LedState.IsOn;
                _workspaceVm.LedToggleCount = _memory.LedState.ToggleCount;
                _workspaceVm.LedLastValue = _memory.LedState.LastValue;
                _workspaceVm.UpdateLedColor(_memory.LedState.IsOn, _cpu?.Halted ?? false);

                if (prevLedOn != _memory.LedState.IsOn)
                {
                    Logger.Trace("Minimal Blink LED changed: {State} (value=0x{Value:X2}, toggles={Toggles})",
                        _memory.LedState.IsOn ? "ON" : "OFF",
                        _memory.LedState.LastValue,
                        _memory.LedState.ToggleCount);
                }
            }

            if (_lcdBuffer is not null && _lcd is not null)
            {
                _lcdBuffer.RenderFrom(_lcd);

                var src = _lcdBuffer.Pixels;
                int w = src.GetLength(0), h = src.GetLength(1);
                var dst = new bool[w, h];
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                        dst[x, y] = src[x, y];
                _workspaceVm.LcdPixels = dst;

                Logger.Debug("Minimal Blink LCD snapshot: PC=0x{PC:X4}, first16={Bytes}",
                    _cpu?.PC ?? 0,
                    string.Join(" ", _lcd.Ddram.Take(16).Select(b => b == 0 ? ".." : $"{(char)b}")));
            }

            if (_terminal is not null)
            {
                var snap = _terminal.GetSnapshot();
                _workspaceVm.TerminalLines = new List<string>(snap.Lines);
            }

            if (snapshot.Rtc is not null)
            {
                _workspaceVm.ShowRtcModule = true;
                _workspaceVm.RtcCurrentTime = snapshot.Rtc.CurrentTime.ToString("yyyy-MM-dd HH:mm:ss");
                _workspaceVm.RtcTimeMode = snapshot.Rtc.TimeMode.ToString();
                _workspaceVm.RtcI2cAddress = snapshot.Rtc.I2cAddress.HasValue
                    ? $"0x{snapshot.Rtc.I2cAddress.Value:X2}"
                    : "-";
                _workspaceVm.RtcBusAddress = snapshot.Rtc.DirectBusBaseAddress.HasValue
                    ? $"0x{snapshot.Rtc.DirectBusBaseAddress.Value:X4} ({snapshot.Rtc.DirectBusMode})"
                    : "-";
                _workspaceVm.RtcRegisters = snapshot.Rtc.Registers
                    .Select((value, index) => new CpuRegisterSnapshot($"0x{index:X2}", $"0x{value:X2}"))
                    .ToList();
            }
            else
            {
                _workspaceVm.ShowRtcModule = false;
                _workspaceVm.RtcCurrentTime = string.Empty;
                _workspaceVm.RtcTimeMode = string.Empty;
                _workspaceVm.RtcI2cAddress = string.Empty;
                _workspaceVm.RtcBusAddress = string.Empty;
                _workspaceVm.RtcRegisters = [];
            }
        };

        if (_uiDispatcher is not null)
            _uiDispatcher.Post(update);
        else
            update();
    }

    public async ValueTask DisposeAsync()
    {
        await PauseAsync();
        _cpu = null;
        _memory = null;
        Logger.Info("Minimal Blink Computer session disposed");
    }

}
