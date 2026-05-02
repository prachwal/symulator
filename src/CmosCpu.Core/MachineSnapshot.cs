namespace CmosCpu.Core;

public sealed record MachineSnapshot(
    string CpuName,
    CpuRegisters Registers,
    bool IsHalted,
    ulong Cycle,
    bool IsRunning
);