using CmosCpu.Computer;
using Symulator.Machines.Apple1.Devices;

namespace Symulator.Machines.Apple1.Factory;

public sealed class Apple1MachineRuntime
{
    public ComputerMachine Machine { get; }
    public Apple1PiaTerminalDevice Terminal { get; }

    public Apple1MachineRuntime(ComputerMachine machine, Apple1PiaTerminalDevice terminal)
    {
        Machine = machine;
        Terminal = terminal;
    }
}
