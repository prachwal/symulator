using CmosCpu.Computer.Devices;
using NLog;
using Symulator.Application.Abstractions;
using Symulator.Machines.MinimalBlink.Cpu;
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
    private MinimalBlinkLcdBuffer? _lcdBuffer;
    private string? _selectedProgramId;
    private string? _loadedProgramId;

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
            $"${_cpu.PC:X4}",  // PC
            $"${_cpu.A:X2}",   // A → pole A
            "--",              // X (unused)
            "--",              // Y (unused)
            "--",              // Flags (unused)
            _cpu.Halted ? "HLT" : "RUN",
            _cpu.CycleCount,
            _cpu.Halted,
            registers);

        return new EmulatorStateSnapshot("minimal-blink", _isRunning, _cpu.Halted, cpuSnap, string.Empty, (long)_cpu.CycleCount);
    }

    private void Initialize()
    {
        if (_isInitialized)
            return;

        _memory = new MinimalBlinkMemory();
        _lcd = new Hd44780Lcd();
        _lcdBus = new Hd44780DirectBusAdapter(_lcd, MinimalBlinkMemory.LcdCommandPort);
        _memory.AttachLcd(_lcd, _lcdBus);
        _lcdBuffer = new MinimalBlinkLcdBuffer();
        _cpu = new MinimalBlinkCpu(_memory);
        _isInitialized = true;
        Logger.Info("Minimal Blink Computer created with LCD");
    }

    private void AutoLoadDefault()
    {
        if (_cpu!.PC != 0)
            return;

        var program = _selectedProgramId is not null
            ? MinimalBlinkPredefinedPrograms.FindById(_selectedProgramId)
            : MinimalBlinkPredefinedPrograms.FindById("blink-led");

        if (program is null)
            return;

        Logger.Debug("Minimal Blink auto-loading program: {Name}", program.Name);
        _memory!.ClearAll();
        _memory.Load(program.LoadAddress, program.Bytes);
        _cpu.PC = program.StartAddress;
        _loadedProgramId = program.Id;
        _selectedProgramId ??= program.Id;
        _status = $"Loaded: {program.Name}";
        Logger.Info("Minimal Blink loaded program: {Name} at ${Start:X4} ({ByteCount} bytes)",
            program.Name, program.StartAddress, program.Bytes.Length);
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        Initialize();
        _memory!.ClearAll();
        _cpu!.Reset();
        _isRunning = false;
        _status = "Reset";
        PublishSnapshot();
        Logger.Info("Minimal Blink Computer reset");
        return Task.CompletedTask;
    }

    public Task StepInstructionAsync(CancellationToken cancellationToken = default)
    {
        if (_cpu is null || _memory is null)
        {
            Initialize();
            AutoLoadDefault();
            return Task.CompletedTask;
        }

        if (_cpu.PC == 0 && !_isRunning)
            AutoLoadDefault();

        _cpu.Step();
        _lcd?.Tick(_cpu.CycleCount);
        _status = _cpu.Halted ? "Halted" : "Stepped";
        PublishSnapshot();
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Initialize();

        if (_runTask is { IsCompleted: false })
            return;

        AutoLoadDefault();

        _isRunning = true;
        _status = "Running";
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        PublishSnapshot();
        Logger.Info("Minimal Blink Computer run started");

        _runTask = Task.Run(async () =>
        {
            try
            {
                while (!_runCts.IsCancellationRequested && !_cpu!.Halted)
                {
                    for (int i = 0; i < _instructionsPerBatch; i++)
                    {
                        if (_runCts.IsCancellationRequested || _cpu.Halted)
                            break;
                        _cpu.Step();
                        _lcd?.Tick(_cpu.CycleCount);
                    }

                    PublishSnapshot();
                    await Task.Delay(_uiRefreshDelayMs, _runCts.Token);
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
                if (_cpu!.Halted) _status = "Halted";
                else _status = "Paused";
                PublishSnapshot();
                Logger.Info("Minimal Blink Computer run stopped");
            }
        }, _runCts.Token);
    }

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        if (_runCts is not null)
        {
            await _runCts.CancelAsync();
            _runCts.Dispose();
            _runCts = null;
        }

        if (_runTask is not null)
        {
            try { await _runTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken); }
            catch (TimeoutException) { Logger.Warn("Minimal Blink run task did not stop within 2s"); }
            _runTask = null;
        }

        _isRunning = false;
        _status = "Paused";
        PublishSnapshot();
        Logger.Info("Minimal Blink Computer paused");
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
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
                    _cpu!.PC = program.StartAddress;
                    _loadedProgramId = program.Id;
                    _selectedProgramId = program.Id;
                    _status = $"Loaded: {program.Name}";
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
                _status = "Cleared";
                PublishSnapshot();
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

            _workspaceVm.UpdateLoadedProgram(_loadedProgramId, _selectedProgramId);
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
