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

    public CpuStepResult StepInstruction()
    {
        var pcBefore = _cpu.Registers.PC;
        ulong cyclesBefore = _cpu.Registers.CycleCount;

        _cpu.Step();

        int stepCycles = (int)(_cpu.Registers.CycleCount - cyclesBefore);

        return new CpuStepResult(
            ProgramCounterBefore: pcBefore,
            ProgramCounterAfter: _cpu.Registers.PC,
            Opcode: 0,
            Cycles: Math.Max(1, stepCycles)
        );
    }

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