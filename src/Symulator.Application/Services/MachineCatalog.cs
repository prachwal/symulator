using Symulator.Application.Abstractions;

namespace Symulator.Application.Services;

public sealed class MachineCatalog : IMachineCatalog
{
    private readonly IReadOnlyList<IMachineModule> _modules;

    public MachineCatalog(IEnumerable<IMachineModule> modules)
    {
        _modules = modules?.ToList() ?? throw new ArgumentNullException(nameof(modules));
    }

    public IReadOnlyList<MachineDescriptor> GetMachines()
    {
        return _modules.SelectMany(m => m.GetMachines()).ToList();
    }

    public IReadOnlyList<IMachineModule> Modules => _modules;

    public IMachineModule? FindModule(string machineId)
    {
        return _modules.FirstOrDefault(m => m.GetMachines().Any(d => d.Id == machineId));
    }
}
