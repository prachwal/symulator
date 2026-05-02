namespace Symulator.Application.Abstractions;

public interface IMachineModule
{
    string Id { get; }
    string DisplayName { get; }
    string Family { get; }
    string Description { get; }
    IReadOnlyList<MachineDescriptor> GetMachines();
    Task<IMachineSession> CreateSessionAsync(string machineId, CancellationToken cancellationToken = default);
}
