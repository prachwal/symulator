using Symulator.Application.Abstractions;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1MachineSession : IMachineSession
{
    private EmulatorStateSnapshot _current = new("kim1", false, false, null, string.Empty, 0);
    private readonly List<IMachinePanelDescriptor> _panels;

    public Kim1MachineSession()
    {
        _panels = new List<IMachinePanelDescriptor>
        {
            new MachinePanelDescriptor("kim1.keypad", "Keypad", "kim1.keypad", new Kim1KeypadViewModel()),
            new MachinePanelDescriptor("kim1.led-display", "LED Display", "kim1.led-display", new Kim1LedDisplayViewModel()),
            new MachinePanelDescriptor("kim1.riot-status", "RIOT Status", "kim1.riot-status", new Kim1RiotStatusViewModel()),
            new MachinePanelDescriptor("common.cpu-state", "CPU", "common.cpu-state", new object())
        };
    }

    public string MachineId => "kim1";
    public string DisplayName => "KIM-1";
    public EmulatorStateSnapshot Current => _current;
    public IReadOnlyList<IMachinePanelDescriptor> Panels => _panels;

    public event EventHandler<EmulatorStateSnapshot>? StateChanged;
    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? StatusChanged;

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _current = _current with { IsRunning = false, IsHalted = false };
        StateChanged?.Invoke(this, _current);
        StatusChanged?.Invoke(this, "KIM-1 reset");
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
        StatusChanged?.Invoke(this, "KIM-1 running");
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken = default)
    {
        _current = _current with { IsRunning = false };
        StateChanged?.Invoke(this, _current);
        StatusChanged?.Invoke(this, "KIM-1 paused");
        return Task.CompletedTask;
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
                _current = _current with { IsRunning = false, IsHalted = false };
                StateChanged?.Invoke(this, _current);
                StatusChanged?.Invoke(this, "KIM-1 reset");
                break;

            case "kim1.clear-display":
                _current = _current with { TerminalText = string.Empty };
                StateChanged?.Invoke(this, _current);
                break;

            case "kim1.press-key":
            case "kim1.release-key":
                break;
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
