using CmosCpu.Computer.Devices;
using CmosCpu.Computer.Devices.I2c;
using CmosCpu.Computer.Devices.Rtc;
using CmosCpu.Computer.Devices.Serial;
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
    public const ushort I2cBase = 0xFE30;
    public const ushort I2cEnd = 0xFE33;
    public const ushort UartBase = 0xFE40;
    public const ushort UartEnd = 0xFE42;

    public MinimalBlinkLedState LedState { get; } = new();
    public Hd44780DirectBusAdapter? LcdBus { get; private set; }
    public Hd44780Lcd? LcdDevice { get; private set; }
    public MemoryMappedI2cController? I2cController { get; private set; }
    public MemoryMappedUartAdapter? UartAdapter { get; private set; }
    public RtcBusMappedDevice? RtcBus { get; private set; }

    public void AttachLcd(Hd44780Lcd lcd, Hd44780DirectBusAdapter bus)
    {
        LcdDevice = lcd;
        LcdBus = bus;
    }

    public void AttachI2c(MemoryMappedI2cController controller)
    {
        I2cController = controller;
    }

    public void AttachUart(MemoryMappedUartAdapter adapter)
    {
        UartAdapter = adapter;
    }

    public void AttachRtcBus(RtcBusMappedDevice device)
    {
        RtcBus = device;
    }

    public byte ReadByte(ushort address)
    {
        if (address <= RamEnd)
            return _ram[address];
        if (address >= RomStart && address <= RomEnd)
            return _rom[address - RomStart];
        if (LcdBus is not null && (address == LcdCommandPort || address == LcdDataPort))
            return LcdBus.Read(address);
        if (I2cController is not null && address >= I2cBase && address <= I2cEnd)
            return I2cController.Read((ushort)(address - I2cBase));
        if (UartAdapter is not null && address >= UartBase && address <= UartEnd)
            return UartAdapter.Read((ushort)(address - UartBase));
        if (RtcBus is not null && RtcBus.Handles(address))
            return RtcBus.Read(address);
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
        else if (LcdBus is not null && (address == LcdCommandPort || address == LcdDataPort))
            LcdBus.Write(address, value);
        else if (I2cController is not null && address >= I2cBase && address <= I2cEnd)
            I2cController.Write((ushort)(address - I2cBase), value);
        else if (UartAdapter is not null && address >= UartBase && address <= UartEnd)
            UartAdapter.Write((ushort)(address - UartBase), value);
        else if (RtcBus is not null && RtcBus.Handles(address))
            RtcBus.Write(address, value);
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
