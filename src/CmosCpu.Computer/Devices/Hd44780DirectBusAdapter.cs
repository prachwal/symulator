using CmosCpu.Computer.Abstractions;

namespace CmosCpu.Computer.Devices;

/// <summary>Direct bus adapter: offset 0 = command/status, offset 1 = data.</summary>
public sealed class Hd44780DirectBusAdapter : IMemoryMappedDevice
{
    private readonly Hd44780Lcd _lcd;

    public ushort StartAddress { get; }
    public ushort EndAddress => (ushort)(StartAddress + 1);

    public Hd44780DirectBusAdapter(Hd44780Lcd lcd, ushort baseAddress = 0xFE00)
    {
        _lcd = lcd;
        StartAddress = baseAddress;
    }

    public byte Read(ushort address)
    {
        bool isData = (address & 1) == 1;
        return isData ? _lcd.ReadData() : _lcd.ReadStatus();
    }

    public void Write(ushort address, byte value)
    {
        bool isData = (address & 1) == 1;
        if (isData)
            _lcd.WriteData(value);
        else
            _lcd.ExecuteInstruction(value);
    }
}
