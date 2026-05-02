using CmosCpu.Core;

namespace CmosCpu.Computer;

public sealed class ComputerMemoryBus : IMemoryBus
{
    private readonly List<MappedRange> _ranges = [];

    public byte OpenBusValue { get; set; } = 0xFF;

    public void MapRam(ushort start, ushort size)
    {
        var data = new byte[size];
        _ranges.Add(new MappedRange(
            start, (ushort)(start + size - 1),
            addr => data[addr - start],
            (addr, val) => data[addr - start] = val,
            data));
    }

    public void MapRom(ushort start, ushort size)
    {
        var data = new byte[size];
        _ranges.Add(new MappedRange(
            start, (ushort)(start + size - 1),
            addr => data[addr - start],
            (addr, val) => { },
            data));
    }

    public void MapDevice(ushort start, ushort size, Func<ushort, byte> read, Action<ushort, byte> write)
    {
        _ranges.Add(new MappedRange(start, (ushort)(start + size - 1), read, write, null));
    }

    public void LoadRom(ushort start, byte[] data)
    {
        var range = _ranges.Find(r => start >= r.Start && start <= r.End);
        if (range?.Data is null)
            return;
        int offset = start - range.Start;
        int count = Math.Min(data.Length, range.Data.Length - offset);
        Array.Copy(data, 0, range.Data, offset, count);
    }

    public byte ReadByte(ushort address)
    {
        var range = _ranges.Find(r => address >= r.Start && address <= r.End);
        if (range is null)
            return OpenBusValue;
        return range.Read(address);
    }

    public void WriteByte(ushort address, byte value)
    {
        var range = _ranges.Find(r => address >= r.Start && address <= r.End);
        if (range is null)
            return;
        range.Write(address, value);
    }

    public byte[]? GetRamData(ushort start)
    {
        var range = _ranges.Find(r => start >= r.Start && start <= r.End);
        return range?.Data;
    }

    public byte[] GetMemoryPage(ushort start, int length = 256)
    {
        var result = new byte[length];
        for (int i = 0; i < length; i++)
        {
            ushort addr = (ushort)(start + i);
            result[i] = ReadByte(addr);
        }
        return result;
    }

    private sealed record MappedRange(
        ushort Start,
        ushort End,
        Func<ushort, byte> Read,
        Action<ushort, byte> Write,
        byte[]? Data);
}
