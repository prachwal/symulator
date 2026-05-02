using Symulator.Application.Abstractions;
using Symulator.Machines.Apple1.Models;
using Symulator.Machines.Apple1.Services;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1MachineModule : IMachineModule
{
    public string Id => "apple1";
    public string DisplayName => "Apple-1";
    public string Family => "Apple";
    public string Description => "Apple-1 (1976) with Woz Monitor and BASIC";

    public IReadOnlyList<MachineDescriptor> GetMachines()
    {
        return new List<MachineDescriptor>
        {
            new("apple1", "Apple-1", "Apple", "profiles/apple1.json")
        };
    }

    public Task<IMachineSession> CreateSessionAsync(string machineId, CancellationToken cancellationToken = default)
    {
        if (machineId != "apple1")
            throw new ArgumentException($"Unknown machine: {machineId}", nameof(machineId));

        var session = new Apple1MachineSession();
        return Task.FromResult<IMachineSession>(session);
    }
}
