using Symulator.Machines.MinimalBlink.Models;

namespace Symulator.Machines.MinimalBlink.Memory;

public sealed class MinimalBlinkMemory
{
    private readonly byte[] _ram = new byte[256];
    private readonly byte[] _rom = new byte[256];

    public const ushort RamStart = 0x0000;
    public const ushort RamEnd = 0x00FF;
    public const ushort RomStart = 0x0100;
    public const ushort RomEnd = 0x01FF;
    public const ushort LedPort = 0xFF00;

    public MinimalBlinkLedState LedState { get; } = new();

    public byte ReadByte(ushort address)
    {
        if (address <= RamEnd)
            return _ram[address];
        if (address >= RomStart && address <= RomEnd)
            return _rom[address - RomStart];
        return 0;
    }

    public void WriteByte(ushort address, byte value)
    {
        if (address <= RamEnd)
            _ram[address] = value;
        else if (address >= RomStart && address <= RomEnd)
            _rom[address - RomStart] = value;
        else if (address == LedPort)
            LedState.Write(value);
    }

    public void Load(ushort start, byte[] data)
    {
        for (int i = 0; i < data.Length && (start + i) <= RomEnd; i++)
            _rom[start + i - RomStart] = data[i];
    }

    public byte GetRamByte(ushort address)
    {
        if (address <= RamEnd)
            return _ram[address];
        return 0;
    }

    public void ClearAll()
    {
        Array.Clear(_ram);
        Array.Clear(_rom);
        LedState.Reset();
    }
}
