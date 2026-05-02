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

    public void Reset()
    {
        ResetCount++;
        Registers.PC = 0x8000;
    }

    public void StepInstruction()
    {
        StepCount++;
        Registers.PC++;
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
