namespace Symulator.Application.Abstractions;

public interface IEmulatorController
{
    IMachineCatalog Catalog { get; }
    IMachineSession? ActiveSession { get; }
    EmulatorStateSnapshot? Current { get; }

    event EventHandler<EmulatorStateSnapshot>? StateChanged;
    event EventHandler<string>? StatusChanged;

    Task<bool> SelectMachineAsync(string machineId, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
    Task StepInstructionAsync(CancellationToken cancellationToken = default);
    Task RunAsync(CancellationToken cancellationToken = default);
    Task PauseAsync(CancellationToken cancellationToken = default);
    Task SendInputAsync(string text, CancellationToken cancellationToken = default);
    Task<MachineCommandResult> ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default);
}
