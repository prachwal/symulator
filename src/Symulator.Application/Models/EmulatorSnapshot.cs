namespace Symulator.Application.Abstractions;

public sealed record MachineDescriptor(string Id, string Name, string Family, string DefaultProfilePath);

public sealed record CpuRegisterSnapshot(string Name, string Value);

public sealed record CpuStateSnapshot(
    string Pc,
    string A,
    string X,
    string Y,
    string Sp,
    string Flags,
    ulong CycleCount,
    bool IsHalted,
    IReadOnlyList<CpuRegisterSnapshot> Registers);

public sealed record EmulatorStateSnapshot(
    string MachineId,
    bool IsRunning,
    bool IsHalted,
    CpuStateSnapshot? Cpu,
    string TerminalText,
    long TotalInstructions);
