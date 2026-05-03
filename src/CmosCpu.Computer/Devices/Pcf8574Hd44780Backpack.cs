using CmosCpu.Computer.Devices.I2c;
using NLog;

namespace CmosCpu.Computer.Devices;

public sealed class Pcf8574Hd44780PinMap
{
    public int RsBit { get; init; } = 0;
    public int RwBit { get; init; } = 1;
    public int EBit { get; init; } = 2;
    public int BacklightBit { get; init; } = 3;
    public int D4Bit { get; init; } = 4;
    public int D5Bit { get; init; } = 5;
    public int D6Bit { get; init; } = 6;
    public int D7Bit { get; init; } = 7;
}

public sealed class Pcf8574Hd44780Backpack : II2cDevice
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Hd44780Parallel4BitAdapter _lcd4Bit;
    private readonly Pcf8574Hd44780PinMap _map;
    private byte _lastValue;

    public byte Address { get; }
    public bool BacklightOn { get; private set; }

    public Pcf8574Hd44780Backpack(
        byte address,
        Hd44780Parallel4BitAdapter lcd4Bit,
        Pcf8574Hd44780PinMap? map = null)
    {
        Address = address;
        _lcd4Bit = lcd4Bit;
        _map = map ?? new Pcf8574Hd44780PinMap();
    }

    public void WriteByte(byte value)
    {
        _lastValue = value;
        bool rs = ((value >> _map.RsBit) & 1) != 0;
        bool rw = ((value >> _map.RwBit) & 1) != 0;
        bool e  = ((value >> _map.EBit) & 1) != 0;
        BacklightOn = ((value >> _map.BacklightBit) & 1) != 0;

        byte d4 = (byte)(((value >> _map.D4Bit) & 1) << 4);
        byte d5 = (byte)(((value >> _map.D5Bit) & 1) << 5);
        byte d6 = (byte)(((value >> _map.D6Bit) & 1) << 6);
        byte d7 = (byte)(((value >> _map.D7Bit) & 1) << 7);
        byte data = (byte)(d4 | d5 | d6 | d7);

        Logger.Trace("PCF8574 value=0x{Value:X2} rs={Rs} rw={Rw} e={E} bl={Bl} nibble=0x{Nibble:X2}",
            value, rs, rw, e, BacklightOn, data);

        var pins = new Hd44780Pins(rs, rw, e, data);
        _lcd4Bit.WritePins(pins);
    }

    public byte ReadByte() => _lastValue;
}
