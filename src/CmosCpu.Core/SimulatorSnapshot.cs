using System.Collections.Generic;

namespace CmosCpu.Core;

public class SimulatorSnapshot
{
    public CpuRegisters Registers { get; init; } = new();
    public ulong CycleCount { get; init; }
    public bool LedOn { get; init; }
    public Dictionary<ushort, byte> MemoryDump { get; init; } = new();
    public string? CurrentInstruction { get; init; }
    public IReadOnlyList<MemoryCellSnapshot> MemoryWindow { get; init; } = System.Array.Empty<MemoryCellSnapshot>();
    public IReadOnlyList<TraceEntry> Trace { get; init; } = System.Array.Empty<TraceEntry>();
    public IReadOnlyList<BusTransaction> BusTransactions { get; init; } = System.Array.Empty<BusTransaction>();
    public TimerSnapshot? Timer { get; init; }
    public RtcSnapshot? Rtc { get; init; }
    public InterruptSnapshot? Interrupts { get; init; }
}
