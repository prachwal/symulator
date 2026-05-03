using CmosCpu.Computer;
using NLog;
using Symulator.Application.Abstractions;
using Symulator.Machines.Apple1.Factory;
using Symulator.Machines.Apple1.Models;
using Symulator.Machines.Apple1.Services;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1MachineSession : IMachineSession
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IMachineNotificationSink? _notificationSink;
    private readonly IUiDispatcher? _uiDispatcher;
    private ComputerMachine? _machine;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private Apple1WorkspaceViewModel? _workspaceVm;
    private bool _isRunning;
    private Apple1BootState _bootState = Apple1BootState.NotInitialized;

    private const int _instructionsPerBatch = 100;
    private const int _uiRefreshDelayMs = 16;

    public Apple1MachineSession(IMachineNotificationSink? notificationSink = null, IUiDispatcher? uiDispatcher = null)
    {
        _notificationSink = notificationSink;
        _uiDispatcher = uiDispatcher;
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
        _workspaceVm ??= new Apple1WorkspaceViewModel(this, _notificationSink);
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
            _notificationSink?.Info($"Creating Apple-1 machine from {moduleDir}");
            Logger.Debug("Apple-1 session EnsureMachineCreated using moduleDir={ModuleDir}", moduleDir);
            _machine = Apple1MachineFactory.Create(moduleDir);
            _bootState = Apple1BootState.Reset;
            Logger.Info("Apple-1 machine created from {ModuleDir}", moduleDir);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create Apple-1 machine");
            _notificationSink?.Error(ex, "Create Apple-1 machine");
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
        Logger.Debug("Apple-1 machine reset complete; bootState={BootState}", _bootState);
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
            const string message = "Machine not initialized. Use Boot MON, Boot BASIC or Reset first.";
            StatusChanged?.Invoke(this, message);
            _notificationSink?.Warning(message);
            return Task.CompletedTask;
        }

        _machine.Step();
        Logger.Debug("Apple-1 single step executed; cycles={Cycles}; pc={Pc}", _machine.Cpu.CycleCount, _machine.Cpu.PC);
        PublishSnapshot();
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_machine is null)
        {
            const string message = "Machine not initialized. Use Boot MON, Boot BASIC or Reset first.";
            StatusChanged?.Invoke(this, message);
            _notificationSink?.Warning(message);
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
                _notificationSink?.Error(ex, "Apple-1 run loop");
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
        if (_machine is null)
        {
            _notificationSink?.Warning("Cannot send input before Apple-1 is initialized");
            return Task.CompletedTask;
        }

        Logger.Debug("Apple-1 SendInputAsync called with text='{Text}'", text.Replace("\r", "\\r").Replace("\n", "\\n"));

        if (_machine.Keyboard is not null)
        {
            var normalized = text.TrimEnd('\r', '\n');
            foreach (char c in normalized) _machine.Keyboard.EnqueueKey(char.ToUpperInvariant(c));
            _machine.Keyboard.EnqueueKey('\r');
        }
        else if (_machine.Apple1Terminal is not null)
        {
            var normalized = text.TrimEnd('\r', '\n');
            foreach (char c in normalized) _machine.Apple1Terminal.QueueKey(char.ToUpperInvariant(c));
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
                var terminalVersionBeforeMonitor = _machine.Apple1Terminal?.Version ?? 0;
                var terminalLengthBeforeMonitor = _machine.Apple1Terminal?.OutputLength ?? 0;
                _bootState = Apple1BootState.BootingMonitor;
                _notificationSink?.Info("Booting Apple-1 Woz Monitor...");
                Logger.Debug("Apple-1 boot.monitor started; terminalVersion={Version}, outputLength={Length}",
                    terminalVersionBeforeMonitor, terminalLengthBeforeMonitor);

                var monitorResult = await RunUntil(
                    () =>
                    {
                        var term = _machine.Apple1Terminal;
                        if (term is null) return false;
                        return term.Version > terminalVersionBeforeMonitor
                            && term.Text.Contains('\\');
                    },
                    50000,
                    "Waiting for monitor prompt",
                    cancellationToken);

                PublishSnapshot();
                string terminal = _machine.Apple1Terminal?.Text ?? string.Empty;

                if (monitorResult && terminal.Contains('\\'))
                {
                    _bootState = Apple1BootState.MonitorReady;
                    if (_workspaceVm is not null) _workspaceVm.ModeText = "MonitorReady";
                    StatusChanged?.Invoke(this, "Woz Monitor ready");
                    Logger.Info("Apple-1 Woz Monitor booted successfully");
                    return MachineCommandResult.Success();
                }

                _bootState = Apple1BootState.Error;
                if (_workspaceVm is not null) _workspaceVm.ModeText = "Error";
                _notificationSink?.Warning("Apple-1 Woz Monitor did not show prompt", terminal);
                return MachineCommandResult.Failure($"Woz Monitor did not show prompt. Terminal output: {terminal[..Math.Min(terminal.Length, 200)]}");

            case "apple1.boot.basic":
                if (!EnsureMachineCreated())
                    return MachineCommandResult.Failure("Cannot create Apple-1 machine");

                _machine!.Reset();
                var terminalVersionBeforeBasic = _machine.Apple1Terminal?.Version ?? 0;
                Logger.Debug("Apple-1 boot.basic: reset + wait for monitor; terminalVersion={Version}", terminalVersionBeforeBasic);

                var monForBasicResult = await RunUntil(
                    () =>
                    {
                        var term = _machine.Apple1Terminal;
                        if (term is null) return false;
                        return term.Version > terminalVersionBeforeBasic
                            && term.Text.Contains('\\');
                    },
                    50000,
                    "Waiting for monitor prompt",
                    cancellationToken);

                if (!monForBasicResult)
                {
                    _bootState = Apple1BootState.Error;
                    if (_workspaceVm is not null) _workspaceVm.ModeText = "Error";
                    Logger.Warn("Apple-1 boot.basic: monitor did not start after reset");
                    return MachineCommandResult.Failure("Monitor did not start after reset, cannot boot BASIC");
                }

                _bootState = Apple1BootState.BootingBasic;
                _notificationSink?.Info("Booting Apple-1 BASIC...");
                Logger.Debug("Apple-1 boot.basic started after monitor success");

                const string basicCommand = "E000R\r";
                Logger.Debug("Apple-1 BASIC input: sending command '{Command}'", basicCommand.Replace("\r", "\\r"));
                foreach (char c in basicCommand)
                {
                    Logger.Debug("Apple-1 BASIC input char: display='{Display}', raw=0x{Raw:X2}", c == '\r' ? "CR" : c.ToString(), (byte)c);
                    if (_machine.Keyboard is not null)
                        _machine.Keyboard.EnqueueKey(c);
                    else if (_machine.Apple1Terminal is not null)
                        _machine.Apple1Terminal.QueueKey(c);
                }

                var basicResult = await RunUntil(
                    () =>
                    {
                        var text = _machine.Apple1Terminal?.Text ?? string.Empty;
                        return text.Contains("\n>") || text.EndsWith(">");
                    },
                    120000,
                    "Waiting for BASIC prompt",
                    cancellationToken);

                PublishSnapshot();
                string basicTerminal = _machine.Apple1Terminal?.Text ?? string.Empty;
                string basicSuffix = basicTerminal.Length >= 40 ? basicTerminal[^40..] : basicTerminal;
                ushort finalPc = _machine.Cpu.PC;
                ulong finalCycles = _machine.Cpu.CycleCount;

                if (basicResult)
                {
                    Logger.Debug("Apple-1 BASIC boot condition met: pc=0x{Pc:X4}, cycles={Cycles}, terminal={Terminal}",
                        finalPc, finalCycles, basicSuffix.Replace("\n", "\\n"));
                    _bootState = Apple1BootState.BasicReady;
                    if (_workspaceVm is not null) _workspaceVm.ModeText = "BasicReady";
                    StatusChanged?.Invoke(this, "BASIC ready");
                    Logger.Info("Apple-1 BASIC booted successfully");
                    return MachineCommandResult.Success();
                }

                _bootState = Apple1BootState.Error;
                if (_workspaceVm is not null) _workspaceVm.ModeText = "Error";

                byte resetLo = _machine.Memory.ReadByte(0xFFFC);
                byte resetHi = _machine.Memory.ReadByte(0xFFFD);
                ushort resetVector = (ushort)((resetHi << 8) | resetLo);
                byte basicLo = _machine.Memory.ReadByte(0xE000);
                byte basicHi = _machine.Memory.ReadByte(0xE001);
                bool basicRomVisible = basicLo != 0 || basicHi != 0;

                var failureDetail = "";
                if (basicTerminal.Contains("E000:") || basicTerminal.Contains("E000:"))
                    failureDetail = "monitor displayed memory at E000 (E000R did not run, only examined)";
                else if (basicSuffix.Contains("\\") || basicSuffix.EndsWith("\\"))
                    failureDetail = "monitor prompt visible but command was not processed";

                var failureMsg = $"BASIC did not boot. PC=0x{finalPc:X4}, cycles={finalCycles}, " +
                    $"resetVector=0x{resetVector:X4}, basicRomVisible={basicRomVisible} (bytes 0x{basicLo:X2}:0x{basicHi:X2}), " +
                    $"terminal: {basicSuffix.Replace("\n", "\\n")}";
                if (!string.IsNullOrEmpty(failureDetail))
                    failureMsg += $"; {failureDetail}";
                _notificationSink?.Warning(failureMsg);
                Logger.Warn("Apple-1 BASIC boot failed: {Failure}", failureMsg);
                return MachineCommandResult.Failure(failureMsg);

            case "apple1.clear-terminal":
                if (_workspaceVm is not null)
                    _workspaceVm.TerminalOutput = string.Empty;
                StatusChanged?.Invoke(this, "Terminal cleared");
                return MachineCommandResult.Success();

            case "apple1.send-line":
                if (parameter is string line)
                {
                    var inputText = line.TrimEnd('\r', '\n').ToUpperInvariant();

                    if (_machine is not null && !_isRunning && _machine.Apple1Terminal is not null)
                    {
                        Logger.Debug("Apple-1 send-line: interactive processing; bootState={BootState}; input='{Input}'", _bootState, inputText);

                        foreach (char c in inputText)
                        {
                            var versionBeforeChar = _machine.Apple1Terminal.Version;
                            _machine.Apple1Terminal.QueueKey(c);

                            await RunUntil(
                                () => _machine.Apple1Terminal.Version > versionBeforeChar && _machine.Apple1Terminal.PendingKeyCount == 0,
                                10000,
                                $"Processing Apple-1 input char '{c}'",
                                cancellationToken);
                        }

                        var versionBeforeCr = _machine.Apple1Terminal.Version;
                        _machine.Apple1Terminal.QueueKey('\r');

                        var processed = await RunUntil(
                            () =>
                            {
                                var term = _machine.Apple1Terminal;
                                if (term.Version <= versionBeforeCr)
                                    return false;

                                if (term.PendingKeyCount > 0)
                                    return false;

                                return _bootState switch
                                {
                                    Apple1BootState.BasicReady => Apple1PromptDetector.LooksLikeBasicPrompt(term.Text),
                                    Apple1BootState.MonitorReady => Apple1PromptDetector.LooksLikeWozPrompt(term.Text),
                                    _ => true
                                };
                            },
                            50000,
                            "Processing Apple-1 input line terminator",
                            cancellationToken);

                        PublishSnapshot();
                        Logger.Debug("Apple-1 send-line processed={Processed}; pendingKeys={PendingKeys}; pc=0x{Pc:X4}; cycles={Cycles}; terminal={Terminal}",
                            processed,
                            _machine.Apple1Terminal?.PendingKeyCount ?? 0,
                            _machine.Cpu.PC,
                            _machine.Cpu.CycleCount,
                            (_machine.Apple1Terminal?.Text ?? string.Empty).Replace("\n", "\\n"));

                        if (!processed)
                        {
                            Logger.Warn("Apple-1 send-line did not reach stable prompt state; input='{Input}'; pendingKeys={PendingKeys}; pc=0x{Pc:X4}",
                                inputText,
                                _machine.Apple1Terminal?.PendingKeyCount ?? 0,
                                _machine.Cpu.PC);
                        }
                    }
                    else
                    {
                        await SendInputAsync(line, cancellationToken);
                    }

                    return MachineCommandResult.Success();
                }
                return MachineCommandResult.Failure("send-line requires a string parameter");

            default:
                Logger.Warn("Unknown Apple-1 command: {CommandId}", commandId);
                _notificationSink?.Warning($"Unknown Apple-1 command: {commandId}");
                return MachineCommandResult.Failure($"Unknown Apple-1 command: {commandId}");
        }
    }

    private async Task<bool> RunUntil(Func<bool> condition, int maxInstructions, string progressMessage, CancellationToken cancellationToken)
    {
        for (int i = 0; i < maxInstructions; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _machine!.Step();

            if (i % 5000 == 0)
            {
                Logger.Trace("Apple-1 RunUntil progress: {ProgressMessage}; step={Step}; cycles={Cycles}; pc={Pc}", progressMessage, i, _machine!.Cpu.CycleCount, _machine.Cpu.PC);
                PublishSnapshot();
                StatusChanged?.Invoke(this, $"{progressMessage} ({i}/{maxInstructions})");
                await Task.Yield();
            }

            if (condition())
            {
                Logger.Debug("Apple-1 RunUntil condition met: {ProgressMessage}; step={Step}; cycles={Cycles}; pc={Pc}", progressMessage, i, _machine!.Cpu.CycleCount, _machine.Cpu.PC);
                return true;
            }
        }

        Logger.Warn("Apple-1 RunUntil exhausted without meeting condition: {ProgressMessage}; maxInstructions={MaxInstructions}", progressMessage, maxInstructions);
        return false;
    }

    private void PublishSnapshot()
    {
        var snapshot = BuildSnapshot();
        StateChanged?.Invoke(this, snapshot);

        if (_workspaceVm is null) return;

        var modeText = _bootState.ToString();
        var terminalText = snapshot.TerminalText ?? _workspaceVm.TerminalOutput;
        var statusText = _bootState switch
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
            Apple1BootState.Error => "Error - check Error Window / logs",
            _ => _bootState.ToString()
        };

        Action update = () =>
        {
            _workspaceVm.TerminalOutput = terminalText;
            _workspaceVm.ModeText = modeText;
            _workspaceVm.StatusText = statusText;
        };

        if (_uiDispatcher is not null)
            _uiDispatcher.Post(update);
        else
            update();
    }

    public async ValueTask DisposeAsync()
    {
        await PauseAsync();
        _machine = null;
        Logger.Info("Apple-1 session disposed");
    }
}
