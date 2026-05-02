using CmosCpu.Computer;
using NLog;
using Symulator.Application.Abstractions;
using Symulator.Machines.Kim1.Factory;
using Symulator.Machines.Kim1.Services;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1MachineSession : IMachineSession
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private ComputerMachine? _machine;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private Kim1WorkspaceViewModel? _workspaceVm;
    private bool _isRunning;

    private const int _instructionsPerBatch = 100;
    private const int _uiRefreshDelayMs = 16;

    public Kim1MachineSession()
    {
    }

    public string MachineId => "kim1";
    public string DisplayName => "KIM-1";
    public EmulatorStateSnapshot Current => BuildSnapshot();
    public IMachineWorkspaceDescriptor Workspace => GetOrCreateWorkspace();

    public event EventHandler<EmulatorStateSnapshot>? StateChanged;
    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? StatusChanged;

    private IMachineWorkspaceDescriptor GetOrCreateWorkspace()
    {
        _workspaceVm ??= new Kim1WorkspaceViewModel(this);
        return new MachineWorkspaceDescriptor("kim1", "KIM-1", _workspaceVm);
    }

    private EmulatorStateSnapshot BuildSnapshot()
    {
        if (_machine is null)
            return new EmulatorStateSnapshot("kim1", false, false, null, string.Empty, 0);

        var cpu = _machine.Cpu;
        var cpuSnapshot = Kim1MachineFactory.BuildCpuSnapshot(cpu);

        return new EmulatorStateSnapshot("kim1", _isRunning, cpu.IsHalted, cpuSnapshot, string.Empty, (long)cpu.CycleCount);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await PauseAsync(cancellationToken);

        if (_machine is not null)
        {
            _machine.Reset();
            Logger.Info("KIM-1 machine reset");
        }
        else
        {
            try
            {
                var moduleDir = Path.GetDirectoryName(typeof(Kim1MachineModule).Assembly.Location)!;
                _machine = Kim1MachineFactory.Create(moduleDir);
                _machine.Reset();
                Logger.Info("KIM-1 machine created and reset from {ModuleDir}", moduleDir);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create KIM-1 machine");
                StatusChanged?.Invoke(this, $"Error: {ex.Message}");
                return;
            }
        }

        PublishSnapshot();
        UpdateWorkspaceFromRiot();
        StatusChanged?.Invoke(this, "KIM-1 reset");
    }

    public async Task StepInstructionAsync(CancellationToken cancellationToken = default)
    {
        if (_machine is null)
        {
            await ResetAsync(cancellationToken);
            if (_machine is null) return;
        }

        _machine.Step();
        PublishSnapshot();
        UpdateWorkspaceFromRiot();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_machine is null)
        {
            await ResetAsync(cancellationToken);
            if (_machine is null) return;
        }

        if (_runTask is { IsCompleted: false })
            return;

        _isRunning = true;
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        PublishSnapshot();
        UpdateWorkspaceFromRiot();
        StatusChanged?.Invoke(this, "KIM-1 running");
        Logger.Info("KIM-1 run started");

        _runTask = Task.Run(async () =>
        {
            try
            {
                while (!_runCts.IsCancellationRequested && !_machine.Cpu.IsHalted)
                {
                    for (int i = 0; i < _instructionsPerBatch; i++)
                    {
                        if (_runCts.IsCancellationRequested || _machine.Cpu.IsHalted)
                            break;
                        _machine.Step();
                    }

                    PublishSnapshot();
                    UpdateWorkspaceFromRiot();
                    await Task.Delay(_uiRefreshDelayMs, _runCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "KIM-1 run loop error");
                StatusChanged?.Invoke(this, $"Run error: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                PublishSnapshot();
                Logger.Info("KIM-1 run stopped");
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
            try
            {
                await _runTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (TimeoutException)
            {
                Logger.Warn("KIM-1 run task did not stop within 2s");
            }
            _runTask = null;
        }

        _isRunning = false;
        PublishSnapshot();
        UpdateWorkspaceFromRiot();
        StatusChanged?.Invoke(this, "KIM-1 paused");
        Logger.Info("KIM-1 paused");
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<MachineCommandResult> ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default)
    {
        switch (commandId)
        {
            case "kim1.reset":
                await ResetAsync(cancellationToken);
                return MachineCommandResult.Success();

            case "kim1.clear-display":
                PublishSnapshot();
                return MachineCommandResult.Success();

            case "kim1.press-key":
                if (parameter is string keyName)
                {
                    var kimKey = Kim1KeyMapper.MapToKim1Key(keyName);
                    if (kimKey is not null && _machine?.Kim1Keypad is not null)
                    {
                        _machine.Kim1Keypad.PressKey(kimKey);
                        Logger.Debug("KIM-1 key pressed: {Key} -> {KimKey}", keyName, kimKey);
                        return MachineCommandResult.Success();
                    }
                    return MachineCommandResult.Failure($"No keypad or unmapped key: {keyName}");
                }
                return MachineCommandResult.Failure("press-key requires a string parameter");

            case "kim1.release-key":
                if (parameter is string relKeyName)
                {
                    var kimRelKey = Kim1KeyMapper.MapToKim1Key(relKeyName);
                    if (kimRelKey is not null && _machine?.Kim1Keypad is not null)
                    {
                        _machine.Kim1Keypad.ReleaseKey(kimRelKey);
                        Logger.Debug("KIM-1 key released: {Key} -> {KimKey}", relKeyName, kimRelKey);
                        return MachineCommandResult.Success();
                    }
                    return MachineCommandResult.Failure($"No keypad or unmapped key: {relKeyName}");
                }
                return MachineCommandResult.Failure("release-key requires a string parameter");

            default:
                Logger.Warn("Unknown KIM-1 command: {CommandId}", commandId);
                return MachineCommandResult.Failure($"Unknown KIM-1 command: {commandId}");
        }
    }

    private void PublishSnapshot()
    {
        var snapshot = BuildSnapshot();
        StateChanged?.Invoke(this, snapshot);
    }

    private void UpdateWorkspaceFromRiot()
    {
        if (_workspaceVm is null || _machine is null)
            return;

        var riot = Kim1MachineFactory.BuildRiotSnapshot(_machine);
        _workspaceVm.PortA = riot.PortA;
        _workspaceVm.PortB = riot.PortB;
        _workspaceVm.DDRA = riot.DDRA;
        _workspaceVm.DDRB = riot.DDRB;

        if (_machine.Kim1LedDisplay is not null)
            _workspaceVm.DisplayText = _machine.Kim1LedDisplay.Digits;

        _workspaceVm.StatusText = _machine.Cpu.IsHalted
            ? "HALTED"
            : $"PC=${_machine.Cpu.PC:X4} Cycles={_machine.Cpu.CycleCount}";
    }

    public async ValueTask DisposeAsync()
    {
        await PauseAsync();
        _machine = null;
        Logger.Info("KIM-1 session disposed");
    }
}
