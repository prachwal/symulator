using CmosCpu.Computer;
using NLog;
using Symulator.Application.Abstractions;
using Symulator.Machines.Apple1.Factory;
using Symulator.Machines.Apple1.Models;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1MachineSession : IMachineSession
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private ComputerMachine? _machine;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private Apple1WorkspaceViewModel? _workspaceVm;
    private bool _isRunning;
    private Apple1BootState _bootState = Apple1BootState.NotInitialized;

    private const int _instructionsPerBatch = 100;
    private const int _uiRefreshDelayMs = 16;

    public Apple1MachineSession()
    {
    }

    public string MachineId => "apple1";
    public string DisplayName => "Apple-1";
    public EmulatorStateSnapshot Current => BuildSnapshot();
    public IMachineWorkspaceDescriptor Workspace => GetOrCreateWorkspace();

    public event EventHandler<EmulatorStateSnapshot>? StateChanged;
    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? StatusChanged;

    private IMachineWorkspaceDescriptor GetOrCreateWorkspace()
    {
        _workspaceVm ??= new Apple1WorkspaceViewModel(this);
        return new MachineWorkspaceDescriptor("apple1", "Apple-1", _workspaceVm);
    }

    private EmulatorStateSnapshot BuildSnapshot()
    {
        if (_machine is null)
            return new EmulatorStateSnapshot("apple1", false, false, null, string.Empty, 0);

        var cpuSnapshot = Apple1MachineFactory.BuildCpuSnapshot(_machine.Cpu);
        string terminalText = _machine.Apple1Terminal?.Text ?? string.Empty;

        return new EmulatorStateSnapshot("apple1", _isRunning, _machine.Cpu.IsHalted, cpuSnapshot, terminalText, (long)_machine.Cpu.CycleCount);
    }

    private bool EnsureMachineCreated()
    {
        if (_machine is not null) return true;

        try
        {
            var moduleDir = Path.GetDirectoryName(typeof(Apple1MachineModule).Assembly.Location)!;
            _machine = Apple1MachineFactory.Create(moduleDir);
            _bootState = Apple1BootState.Reset;
            Logger.Info("Apple-1 machine created from {ModuleDir}", moduleDir);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create Apple-1 machine");
            _bootState = Apple1BootState.Error;
            StatusChanged?.Invoke(this, $"Error creating machine: {ex.Message}");
            return false;
        }
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (!EnsureMachineCreated())
            return Task.CompletedTask;

        _machine!.Reset();
        _bootState = Apple1BootState.Reset;
        _isRunning = false;

        PublishSnapshot();
        StatusChanged?.Invoke(this, "Apple-1 reset");
        Logger.Info("Apple-1 machine reset");
        return Task.CompletedTask;
    }

    public Task StepInstructionAsync(CancellationToken cancellationToken = default)
    {
        if (_machine is null)
        {
            StatusChanged?.Invoke(this, "Machine not initialized. Use Boot MON, Boot BASIC or Reset first.");
            return Task.CompletedTask;
        }

        _machine.Step();
        PublishSnapshot();
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_machine is null)
        {
            StatusChanged?.Invoke(this, "Machine not initialized. Use Boot MON, Boot BASIC or Reset first.");
            return;
        }

        if (_runTask is { IsCompleted: false })
            return;

        _isRunning = true;
        _bootState = Apple1BootState.Running;
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        PublishSnapshot();
        StatusChanged?.Invoke(this, "Apple-1 running");
        Logger.Info("Apple-1 run started");

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
                    await Task.Delay(_uiRefreshDelayMs, _runCts.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.Error(ex, "Apple-1 run loop error");
                _bootState = Apple1BootState.Error;
                StatusChanged?.Invoke(this, $"Run error: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                if (_machine!.Cpu.IsHalted) _bootState = Apple1BootState.Halted;
                else if (_bootState == Apple1BootState.Running) _bootState = Apple1BootState.Paused;
                PublishSnapshot();
                Logger.Info("Apple-1 run stopped");
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
            catch (TimeoutException) { Logger.Warn("Apple-1 run task did not stop within 2s"); }
            _runTask = null;
        }

        _isRunning = false;
        _bootState = Apple1BootState.Paused;
        PublishSnapshot();
        StatusChanged?.Invoke(this, "Apple-1 paused");
        Logger.Info("Apple-1 paused");
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_machine?.Keyboard is not null)
        {
            foreach (char c in text) _machine.Keyboard.EnqueueKey(c);
            _machine.Keyboard.EnqueueKey('\r');
        }
        else if (_machine?.Apple1Terminal is not null)
        {
            foreach (char c in text) _machine.Apple1Terminal.QueueKey(c);
            _machine.Apple1Terminal.QueueKey('\r');
        }

        PublishSnapshot();
        StatusChanged?.Invoke(this, "Input sent");
        return Task.CompletedTask;
    }

    public async Task<MachineCommandResult> ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default)
    {
        switch (commandId)
        {
            case "apple1.boot.monitor":
                if (!EnsureMachineCreated())
                    return MachineCommandResult.Failure("Cannot create Apple-1 machine");

                _machine!.Reset();
                _bootState = Apple1BootState.BootingMonitor;

                for (int i = 0; i < 2000; i++)
                {
                    _machine.Step();
                    if (_machine.Apple1Terminal?.Text.Contains('\\') == true)
                        break;
                }

                PublishSnapshot();
                string terminal = _machine.Apple1Terminal?.Text ?? string.Empty;

                if (terminal.Contains('\\'))
                {
                    _bootState = Apple1BootState.MonitorReady;
                    if (_workspaceVm is not null) _workspaceVm.ModeText = "MonitorReady";
                    StatusChanged?.Invoke(this, "Woz Monitor ready");
                    Logger.Info("Apple-1 Woz Monitor booted successfully");
                    return MachineCommandResult.Success();
                }

                _bootState = Apple1BootState.Error;
                if (_workspaceVm is not null) _workspaceVm.ModeText = "Error";
                return MachineCommandResult.Failure($"Woz Monitor did not show prompt. Terminal output: {terminal[..Math.Min(terminal.Length, 100)]}");

            case "apple1.boot.basic":
                if (!EnsureMachineCreated())
                    return MachineCommandResult.Failure("Cannot create Apple-1 machine");

                _machine!.Reset();
                _bootState = Apple1BootState.BootingMonitor;

                for (int i = 0; i < 2000; i++)
                {
                    _machine.Step();
                    if (_machine.Apple1Terminal?.Text.Contains('\\') == true)
                        break;
                }

                if (_machine.Apple1Terminal?.Text.Contains('\\') != true)
                {
                    _bootState = Apple1BootState.Error;
                    return MachineCommandResult.Failure("Woz Monitor did not start. Cannot boot BASIC.");
                }

                _bootState = Apple1BootState.BootingBasic;
                if (_machine.Keyboard is not null)
                {
                    foreach (char c in "E00R\r") _machine.Keyboard.EnqueueKey(c);

                    for (int i = 0; i < 5000; i++)
                    {
                        _machine.Step();
                        if (_machine.Apple1Terminal?.Text.Contains('>') == true)
                            break;
                    }
                }

                PublishSnapshot();
                string basicTerminal = _machine.Apple1Terminal?.Text ?? string.Empty;

                if (basicTerminal.Contains('>'))
                {
                    _bootState = Apple1BootState.BasicReady;
                    if (_workspaceVm is not null) _workspaceVm.ModeText = "BasicReady";
                    StatusChanged?.Invoke(this, "BASIC ready");
                    Logger.Info("Apple-1 BASIC booted successfully");
                    return MachineCommandResult.Success();
                }

                _bootState = Apple1BootState.Error;
                if (_workspaceVm is not null) _workspaceVm.ModeText = "Error";
                return MachineCommandResult.Failure($"BASIC did not show prompt. Terminal: {basicTerminal[..Math.Min(basicTerminal.Length, 100)]}");

            case "apple1.clear-terminal":
                if (_workspaceVm is not null)
                    _workspaceVm.TerminalOutput = string.Empty;
                StatusChanged?.Invoke(this, "Terminal cleared");
                return MachineCommandResult.Success();

            case "apple1.send-line":
                if (parameter is string line)
                {
                    await SendInputAsync(line, cancellationToken);
                    return MachineCommandResult.Success();
                }
                return MachineCommandResult.Failure("send-line requires a string parameter");

            default:
                Logger.Warn("Unknown Apple-1 command: {CommandId}", commandId);
                return MachineCommandResult.Failure($"Unknown Apple-1 command: {commandId}");
        }
    }

    private void PublishSnapshot()
    {
        var snapshot = BuildSnapshot();
        StateChanged?.Invoke(this, snapshot);

        if (_workspaceVm is not null)
        {
            _workspaceVm.TerminalOutput = snapshot.TerminalText ?? _workspaceVm.TerminalOutput;
            _workspaceVm.StatusText = _bootState switch
            {
                Apple1BootState.NotInitialized => "Machine not initialized - click Boot MON, Boot BASIC or Reset",
                Apple1BootState.Reset => "Ready - click Boot MON or Boot BASIC",
                Apple1BootState.BootingMonitor => "Booting Woz Monitor...",
                Apple1BootState.MonitorReady => "Woz Monitor ready - enter commands below",
                Apple1BootState.BootingBasic => "Booting BASIC...",
                Apple1BootState.BasicReady => "BASIC ready - enter commands below",
                Apple1BootState.Running => "Running",
                Apple1BootState.Paused => "Paused",
                Apple1BootState.Halted => "HALTED",
                Apple1BootState.Error => "Error - check Error Window",
                _ => _bootState.ToString()
            };
        }
    }

    public async ValueTask DisposeAsync()
    {
        await PauseAsync();
        _machine = null;
        Logger.Info("Apple-1 session disposed");
    }
}
