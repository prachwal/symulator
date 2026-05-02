namespace CmosCpu.Core;

public interface IMachine : IResettable
{
    ICpuCore Cpu { get; }
    IBus Bus { get; }
    ulong Cycle { get; }
    bool IsRunning { get; }

    CpuStepResult StepInstruction();
    void StepCycle();
    void Run(ulong maxCycles);
    void Stop();

    MachineSnapshot GetSnapshot();

    event EventHandler<MachineSnapshot>? SnapshotChanged;
}