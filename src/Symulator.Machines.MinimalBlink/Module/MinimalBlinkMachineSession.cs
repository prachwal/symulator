using NLog;
using Symulator.Application.Abstractions;
using Symulator.Machines.MinimalBlink.Cpu;
using Symulator.Machines.MinimalBlink.Memory;
using Symulator.Machines.MinimalBlink.Models;
using Symulator.Machines.MinimalBlink.Programs;

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

    private const int _instructionsPerBatch = 100;
    private const int _uiRefreshDelayMs = 16;

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
            $"${_cpu.PC:X4}", $"${_cpu.A:X2}", "--", "--",
            "--", _cpu.Halted ? "HLT" : "RUN", _cpu.CycleCount, _cpu.Halted, registers);

        return new EmulatorStateSnapshot("minimal-blink", _isRunning, _cpu.Halted, cpuSnap, string.Empty, (long)_cpu.CycleCount);
    }

    private void Initialize()
    {
        if (_isInitialized)
            return;

        _memory = new MinimalBlinkMemory();
        _cpu = new MinimalBlinkCpu(_memory);
        _isInitialized = true;
        Logger.Info("Minimal Blink Computer created");
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
            return Task.CompletedTask;
        }

        _cpu.Step();
        PublishSnapshot();
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Initialize();

        if (_runTask is { IsCompleted: false })
            return;

        // Auto-load selected program if nothing is loaded (PC still at 0)
        if (_cpu!.PC == 0)
        {
            var defaultProgram = Programs.MinimalBlinkPredefinedPrograms.FindById("blink-led");
            if (defaultProgram is not null)
            {
                Logger.Debug("Minimal Blink auto-loading default program: {Name}", defaultProgram.Name);
                _memory!.ClearAll();
                _memory.Load(defaultProgram.LoadAddress, defaultProgram.Bytes);
                _cpu.PC = defaultProgram.StartAddress;
                _status = $"Loaded: {defaultProgram.Name}";
                Logger.Info("Minimal Blink loaded program: {Name} at ${Start:X4} ({ByteCount} bytes)",
                    defaultProgram.Name, defaultProgram.StartAddress, defaultProgram.Bytes.Length);
            }
        }

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

                    _memory!.ClearAll();
                    _memory.Load(program.LoadAddress, program.Bytes);
                    _cpu!.PC = program.StartAddress;
                    _status = $"Loaded: {program.Name}";
                    Logger.Info("Minimal Blink loaded program: {Name} at ${Start:X4} ({ByteCount} bytes)",
                        program.Name, program.StartAddress, program.Bytes.Length);
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

        if (_workspaceVm is not null)
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
        }
    }

    public async ValueTask DisposeAsync()
    {
        await PauseAsync();
        _cpu = null;
        _memory = null;
        Logger.Info("Minimal Blink Computer session disposed");
    }
}
