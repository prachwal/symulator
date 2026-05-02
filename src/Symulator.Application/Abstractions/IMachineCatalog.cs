namespace Symulator.Application.Abstractions;

public interface IMachineCatalog
{
    IReadOnlyList<MachineDescriptor> GetMachines();
}
