namespace CmosCpu.Core;

public sealed class MemoryRegion
{
    public required string Name { get; init; }
    public ushort Start { get; init; }
    public ushort End { get; init; }
    public required string DeviceName { get; init; }

    public bool Contains(ushort address) => address >= Start && address <= End;
}
