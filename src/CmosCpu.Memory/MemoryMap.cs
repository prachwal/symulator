using CmosCpu.Core;

namespace CmosCpu.Memory;

public sealed class MemoryMap : IMemoryMap
{
    private readonly List<MemoryRegion> _regions;

    public IReadOnlyList<MemoryRegion> Regions => _regions.AsReadOnly();

    public MemoryMap(IEnumerable<MemoryRegion> regions)
    {
        var list = regions.ToList();

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Start > list[i].End)
                throw new ArgumentException($"Region '{list[i].Name}': Start (0x{list[i].Start:X4}) > End (0x{list[i].End:X4})");

            for (int j = 0; j < i; j++)
            {
                if (list[i].Start <= list[j].End && list[j].Start <= list[i].End)
                    throw new ArgumentException($"Region '{list[i].Name}' overlaps with '{list[j].Name}'");
            }
        }

        _regions = list;
    }

    public MemoryRegion? FindRegion(ushort address)
    {
        foreach (var region in _regions)
        {
            if (region.Contains(address))
                return region;
        }
        return null;
    }
}