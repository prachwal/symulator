using Symulator.Application.Abstractions;

namespace Symulator.Machines.MinimalBlink.Module;

public sealed class MinimalBlinkMachineModule : IMachineModule
{
    public string Id => "minimal-blink";
    public string DisplayName => "Minimal Blink Computer";
    public string Family => "educational-8bit";
    public string Description => "Educational 8-bit computer with a tiny custom CPU, RAM, ROM and memory-mapped LED output.";

    public IReadOnlyList<MachineDescriptor> GetMachines()
    {
        return [new MachineDescriptor("minimal-blink", "Minimal Blink Computer", Family, "")];
    }

    public Task<IMachineSession> CreateSessionAsync(string machineId, CancellationToken cancellationToken = default)
    {
        if (machineId != "minimal-blink")
            throw new ArgumentException($"Unknown machine: {machineId}", nameof(machineId));

        return Task.FromResult<IMachineSession>(new MinimalBlinkMachineSession());
    }
}
