namespace CmosCpu.Core;

public class TraceEntry
{
    public ulong Cycle { get; init; }
    public ushort PC { get; init; }
    public byte Opcode { get; init; }
    public string Mnemonic { get; init; } = string.Empty;
    public string Operands { get; init; } = string.Empty;
    public byte A { get; init; }
    public byte X { get; init; }
    public byte Y { get; init; }
    public ushort SP { get; init; }
    public string Flags { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsHalted { get; init; }
}
