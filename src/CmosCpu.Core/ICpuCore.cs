namespace CmosCpu.Core;

public interface ICpuCore : IClockedDevice, IResettable
{
    string Name { get; }
    CpuRegisters Registers { get; }
    bool IsHalted { get; }

    void StepInstruction();
    void RequestInterrupt(InterruptType type);
}
