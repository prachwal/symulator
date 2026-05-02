using CmosCpu.Core;

namespace CmosCpu.Runtime.Tests;

public sealed class FakeCpu : ICpuCore
{
    public string Name => "Fake CPU";
    public CpuRegisters Registers { get; } = new();
    public bool IsHalted => HaltedAfterSteps > 0 && StepCount >= HaltedAfterSteps;

    public int ResetCount { get; set; }
    public int StepCount { get; set; }
    public int HaltedAfterSteps { get; set; } = int.MaxValue;
    public ulong LastTickCycle { get; set; }
    public int CyclesPerStep { get; set; } = 1;

    public void Reset()
    {
        ResetCount++;
        Registers.PC = 0x8000;
        Registers.CycleCount = 0;
    }

    public CpuStepResult StepInstruction()
    {
        var pcBefore = Registers.PC;
        StepCount++;
        Registers.PC++;
        Registers.CycleCount += (ulong)CyclesPerStep;

        return new CpuStepResult(
            ProgramCounterBefore: pcBefore,
            ProgramCounterAfter: Registers.PC,
            Opcode: 0x00,
            Cycles: CyclesPerStep
        );
    }

    public void Tick(ulong cycle)
    {
        LastTickCycle = cycle;
    }

    public void RequestInterrupt(InterruptType type)
    {
    }
}

public sealed class FakeClockedDevice : IClockedDevice
{
    public int TickCount { get; set; }
    public ulong LastCycle { get; set; }

    public void Tick(ulong cycle)
    {
        TickCount++;
        LastCycle = cycle;
    }
}