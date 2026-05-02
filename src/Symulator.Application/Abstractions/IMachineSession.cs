namespace Symulator.Application.Abstractions;

public interface IMachineSession : IAsyncDisposable
{
    string MachineId { get; }
    string DisplayName { get; }
    EmulatorStateSnapshot Current { get; }
    IMachineWorkspaceDescriptor Workspace { get; }

    event EventHandler<EmulatorStateSnapshot>? StateChanged;
    event EventHandler<string>? OutputReceived;
    event EventHandler<string>? StatusChanged;

    Task ResetAsync(CancellationToken cancellationToken = default);
    Task StepInstructionAsync(CancellationToken cancellationToken = default);
    Task RunAsync(CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task SendInputAsync(string text, CancellationToken cancellationToken = default);
    Task<MachineCommandResult> ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default);
}
