using Symulator.Application.Abstractions;
using Symulator.Machines.Apple1.Services;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1MachineSession : IMachineSession
{
    private readonly Apple1InputCoordinator _inputCoordinator = new();
    private EmulatorStateSnapshot _current = new("apple1", false, false, null, string.Empty, 0);
    private readonly List<IMachinePanelDescriptor> _panels;

    public Apple1MachineSession()
    {
        _panels = new List<IMachinePanelDescriptor>
        {
            new MachinePanelDescriptor("apple1.terminal", "Terminal", "apple1.terminal", new Apple1TerminalPanelViewModel(_inputCoordinator)),
            new MachinePanelDescriptor("apple1.boot", "Boot", "apple1.boot", new Apple1BootPanelViewModel()),
            new MachinePanelDescriptor("common.cpu-state", "CPU", "common.cpu-state", new object())
        };
    }

    public string MachineId => "apple1";
    public string DisplayName => "Apple-1";
    public EmulatorStateSnapshot Current => _current;
    public IReadOnlyList<IMachinePanelDescriptor> Panels => _panels;

    public event EventHandler<EmulatorStateSnapshot>? StateChanged;
    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? StatusChanged;

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _inputCoordinator.Reset();
        _current = _current with { IsRunning = false, IsHalted = false };
        StateChanged?.Invoke(this, _current);
        StatusChanged?.Invoke(this, "Apple-1 reset");
        return Task.CompletedTask;
    }

    public Task StepInstructionAsync(CancellationToken cancellationToken = default)
    {
        _current = _current with { TotalInstructions = _current.TotalInstructions + 1 };
        StateChanged?.Invoke(this, _current);
        return Task.CompletedTask;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        _current = _current with { IsRunning = true };
        StateChanged?.Invoke(this, _current);
        StatusChanged?.Invoke(this, "Apple-1 running");
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        _current = _current with { IsRunning = false };
        StateChanged?.Invoke(this, _current);
        StatusChanged?.Invoke(this, "Apple-1 paused");
        return Task.CompletedTask;
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        _inputCoordinator.EnqueueUserLine(text);
        return Task.CompletedTask;
    }

    public Task ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default)
    {
        switch (commandId)
        {
            case "apple1.boot.monitor":
                _inputCoordinator.Reset();
                _current = _current with { TerminalText = "\\\n" };
                StateChanged?.Invoke(this, _current);
                StatusChanged?.Invoke(this, "Woz Monitor started");
                break;

            case "apple1.boot.basic":
                _inputCoordinator.Reset();
                _current = _current with { TerminalText = "> \n" };
                StateChanged?.Invoke(this, _current);
                StatusChanged?.Invoke(this, "BASIC started");
                break;

            case "apple1.clear-terminal":
                _current = _current with { TerminalText = string.Empty };
                StateChanged?.Invoke(this, _current);
                break;

            case "apple1.send-line":
                if (parameter is string line)
                    _inputCoordinator.EnqueueUserLine(line);
                break;
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
