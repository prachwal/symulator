using CmosCpu.Computer;
using Symulator.Machines.Kim1.Devices;

namespace Symulator.Machines.Kim1.Factory;

public sealed class Kim1MachineRuntime
{
    public ComputerMachine Machine { get; }
    public Kim1MachineDevices Devices { get; }

    public Kim1MachineRuntime(ComputerMachine machine, Kim1MachineDevices devices)
    {
        Machine = machine;
        Devices = devices;
    }
}
