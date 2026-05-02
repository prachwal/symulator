using CmosCpu.Computer;
using Symulator.Application.Abstractions;
using Symulator.Machines.Kim1.Services;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1MachineSession : IMachineSession
{
    private ComputerMachine? _machine;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private Kim1WorkspaceViewModel? _workspaceVm;

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
        {
            return new EmulatorStateSnapshot("kim1", false, false, null, string.Empty, 0);
        }

        var cpu = _machine.Cpu;
        var cpuSnapshot = Kim1MachineFactory.BuildCpuSnapshot(cpu);

        return new EmulatorStateSnapshot("kim1", IsRunning, cpu.IsHalted, cpuSnapshot, string.Empty, (long)cpu.CycleCount);
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
            _machine = Kim1MachineFactory.Create(baseDir);
            _machine.Reset();
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
        }

        _machine!.Step();
        PublishSnapshot();
        UpdateWorkspaceFromRiot();
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
                    UpdateWorkspaceFromRiot();
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

        StatusChanged?.Invoke(this, "KIM-1 running");
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
        UpdateWorkspaceFromRiot();
        StatusChanged?.Invoke(this, "KIM-1 paused");
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default)
    {
        switch (commandId)
        {
            case "kim1.reset":
                _ = ResetAsync(cancellationToken);
                break;

            case "kim1.clear-display":
                PublishSnapshot();
                break;

            case "kim1.press-key":
                if (parameter is string keyName)
                {
                    var kimKey = Kim1KeyMapper.MapToKim1Key(keyName);
                    if (kimKey is not null && _machine?.Kim1Keypad is not null)
                    {
                        _machine.Kim1Keypad.PressKey(kimKey);
                    }
                }
                break;

            case "kim1.release-key":
                if (parameter is string relKeyName)
                {
                    var kimRelKey = Kim1KeyMapper.MapToKim1Key(relKeyName);
                    if (kimRelKey is not null && _machine?.Kim1Keypad is not null)
                    {
                        _machine.Kim1Keypad.ReleaseKey(kimRelKey);
                    }
                }
                break;
        }

        return Task.CompletedTask;
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
        {
            _workspaceVm.DisplayText = _machine.Kim1LedDisplay.Digits;
        }

        if (_machine.Cpu is not null)
        {
            _workspaceVm.StatusText = _machine.Cpu.IsHalted
                ? "HALTED"
                : $"PC=${_machine.Cpu.PC:X4} Cycles={_machine.Cpu.CycleCount}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        await PauseAsync();
        _machine = null;
    }
}
