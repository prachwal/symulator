using CmosCpu.Core;

namespace CmosCpu.Runtime;

public sealed class CmosCpuCoreAdapter : ICpuCore
{
    private readonly Cpu.CpuCore _cpu;

    public string Name => "Educational CMOS 8-bit CPU";
    public CpuRegisters Registers => _cpu.Registers;
    public bool IsHalted => _cpu.Halted;

    public CmosCpuCoreAdapter(Cpu.CpuCore cpu)
    {
        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
    }

    public void Reset() => _cpu.Reset();
    public void StepInstruction() => _cpu.Step();
    public void Tick(ulong cycle) { }

    public void RequestInterrupt(InterruptType type)
    {
        switch (type)
        {
            case InterruptType.Reset:
                _cpu.Reset();
                break;
            case InterruptType.Irq:
                _cpu.RequestInterrupt();
                break;
            case InterruptType.Nmi:
                _cpu.RequestNmi();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}
