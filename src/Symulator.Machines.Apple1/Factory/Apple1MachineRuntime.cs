using CmosCpu.Computer;
using CmosCpu.Computer.Devices;
using Symulator.Machines.Apple1.Devices;

namespace Symulator.Machines.Apple1.Factory;

public sealed class Apple1MachineRuntime
{
    public ComputerMachine Machine { get; }
    public Apple1PiaTerminalDevice? Terminal { get; }
    public Pia6821 Pia { get; }
    public Apple1PiaWiring Wiring { get; }
    public Apple1TerminalBuffer Buffer { get; }

    public Apple1MachineRuntime(
        ComputerMachine machine,
        Pia6821 pia,
        Apple1PiaWiring wiring,
        Apple1TerminalBuffer buffer)
    {
        Machine = machine;
        Pia = pia;
        Wiring = wiring;
        Buffer = buffer;
    }
}
