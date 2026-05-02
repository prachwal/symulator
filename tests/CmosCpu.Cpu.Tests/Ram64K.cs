using CmosCpu.Core;

namespace CmosCpu.Cpu.Tests;

public sealed class Ram64K : IMemoryBus
{
    private readonly byte[] _ram = new byte[65536];

    public byte ReadByte(ushort address) => _ram[address];

    public void WriteByte(ushort address, byte value) => _ram[address] = value;

    public void LoadBytes(ushort address, byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
            _ram[address + i] = data[i];
    }

    public void Clear()
    {
        Array.Clear(_ram);
    }
}
