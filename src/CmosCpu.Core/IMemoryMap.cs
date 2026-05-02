namespace CmosCpu.Core;

public interface IMemoryMap
{
    IReadOnlyList<MemoryRegion> Regions { get; }
    MemoryRegion? FindRegion(ushort address);
}
