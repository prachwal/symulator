using System.Collections.Generic;

namespace CmosCpu.Core;

public class SimulatorSnapshot
{
    public CpuRegisters Registers { get; init; } = new();
    public ulong CycleCount { get; init; }
    public bool LedOn { get; init; }
    public Dictionary<ushort, byte> MemoryDump { get; init; } = new();
}
