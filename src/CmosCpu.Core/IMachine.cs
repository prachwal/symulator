namespace CmosCpu.Core;

public interface IMachine : IResettable
{
    ICpuCore Cpu { get; }
    IBus Bus { get; }
    ulong Cycle { get; }
    bool IsRunning { get; }

    void StepInstruction();
    void StepCycle();
    void Run(ulong maxCycles);
    void Stop();
}
