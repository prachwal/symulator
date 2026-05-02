using CmosCpu.Computer;
using Symulator.Application.Abstractions;
using Symulator.Machines.Apple1.Services;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1MachineSession : IMachineSession
{
    private ComputerMachine? _machine;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private Apple1WorkspaceViewModel? _workspaceVm;

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
        {
            return new EmulatorStateSnapshot("apple1", false, false, null, string.Empty, 0);
        }

        var cpu = _machine.Cpu;
        var cpuSnapshot = Apple1MachineFactory.BuildCpuSnapshot(cpu);

        string terminalText = string.Empty;
        if (_machine.Apple1Terminal is not null)
        {
            terminalText = _machine.Apple1Terminal.Text;
        }

        return new EmulatorStateSnapshot("apple1", IsRunning, cpu.IsHalted, cpuSnapshot, terminalText, (long)cpu.CycleCount);
    }

    private bool IsRunning => _runTask is { IsCompleted: false };

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await PauseAsync(cancellationToken);

        if (_machine is not null)
        {
            _machine.Reset();
        }
        else
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _machine = Apple1MachineFactory.Create(baseDir);
            _machine.Reset();
        }

        PublishSnapshot();
        StatusChanged?.Invoke(this, "Apple-1 reset");
    }

    public async Task StepInstructionAsync(CancellationToken cancellationToken = default)
    {
        if (_machine is null)
        {
            await ResetAsync(cancellationToken);
        }

        _machine!.Step();
        PublishSnapshot();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_machine is null)
        {
            await ResetAsync(cancellationToken);
        }

        if (_runTask is { IsCompleted: false })
            return;

        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _runTask = Task.Run(async () =>
        {
            try
            {
                while (!_runCts.IsCancellationRequested && !_machine!.Cpu.IsHalted)
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
            catch (OperationCanceledException)
            {
            }
            finally
            {
                PublishSnapshot();
            }
        }, _runCts.Token);

        StatusChanged?.Invoke(this, "Apple-1 running");
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
            }
            _runTask = null;
        }

        PublishSnapshot();
        StatusChanged?.Invoke(this, "Apple-1 paused");
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_machine?.Keyboard is not null)
        {
            foreach (char c in text)
                _machine.Keyboard.EnqueueKey(c);
            _machine.Keyboard.EnqueueKey('\r');
        }
        else if (_machine?.Apple1Terminal is not null)
        {
            foreach (char c in text)
                _machine.Apple1Terminal.QueueKey(c);
            _machine.Apple1Terminal.QueueKey('\r');
        }

        return Task.CompletedTask;
    }

    public async Task ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default)
    {
        switch (commandId)
        {
            case "apple1.boot.monitor":
                await ResetAsync(cancellationToken);
                for (int i = 0; i < 500; i++)
                {
                    _machine?.Step();
                }
                PublishSnapshot();
                if (_workspaceVm is not null) _workspaceVm.ModeText = "WozMonitor";
                StatusChanged?.Invoke(this, "Woz Monitor started");
                break;

            case "apple1.boot.basic":
                await ResetAsync(cancellationToken);
                for (int i = 0; i < 1000; i++)
                {
                    _machine?.Step();
                }
                if (_machine?.Keyboard is not null)
                {
                    _machine.Keyboard.EnqueueKey('E');
                    _machine.Keyboard.EnqueueKey('0');
                    _machine.Keyboard.EnqueueKey('0');
                    _machine.Keyboard.EnqueueKey('R');
                    _machine.Keyboard.EnqueueKey('\r');
                    for (int i = 0; i < 2000; i++)
                    {
                        _machine?.Step();
                    }
                }
                PublishSnapshot();
                if (_workspaceVm is not null) _workspaceVm.ModeText = "Basic";
                StatusChanged?.Invoke(this, "BASIC started");
                break;

            case "apple1.clear-terminal":
                PublishSnapshot();
                break;

            case "apple1.send-line":
                if (parameter is string line)
                    await SendInputAsync(line, cancellationToken);
                break;
        }
    }

    private void PublishSnapshot()
    {
        var snapshot = BuildSnapshot();
        StateChanged?.Invoke(this, snapshot);

        if (_workspaceVm is not null)
        {
            _workspaceVm.TerminalOutput = snapshot.TerminalText ?? _workspaceVm.TerminalOutput;

            if (snapshot.Cpu is not null)
            {
                _workspaceVm.StatusText = snapshot.Cpu.IsHalted ? "HALTED" : $"PC={snapshot.Cpu.Pc} Cycles={snapshot.Cpu.CycleCount}";
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await PauseAsync();
        _machine = null;
    }
}
