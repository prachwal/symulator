namespace CmosCpu.Core;

public readonly record struct CpuStepResult(
    ushort ProgramCounterBefore,
    ushort ProgramCounterAfter,
    byte Opcode,
    int Cycles
);