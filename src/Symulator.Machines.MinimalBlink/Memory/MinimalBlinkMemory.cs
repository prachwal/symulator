using CmosCpu.Computer.Devices;
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
    public const ushort LcdCommandPort = 0xFE00;
    public const ushort LcdDataPort = 0xFE01;

    public MinimalBlinkLedState LedState { get; } = new();
    public Hd44780Lcd? LcdDevice { get; private set; }

    public void AttachLcd(Hd44780Lcd lcd)
    {
        LcdDevice = lcd;
    }

    public byte ReadByte(ushort address)
    {
        if (address <= RamEnd)
            return _ram[address];
        if (address >= RomStart && address <= RomEnd)
            return _rom[address - RomStart];
        if (LcdDevice is not null && (address == LcdCommandPort || address == LcdDataPort))
            return LcdDevice.Read(address);
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
        else if (LcdDevice is not null && (address == LcdCommandPort || address == LcdDataPort))
            LcdDevice.Write(address, value);
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
        LcdDevice?.Reset();
    }
}
