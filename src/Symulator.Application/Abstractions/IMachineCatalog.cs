namespace Symulator.Application.Abstractions;

public interface IMachineCatalog
{
    IReadOnlyList<MachineDescriptor> GetMachines();
    IMachineModule? FindModule(string machineId);
    IReadOnlyList<IMachineModule> Modules { get; }
}
