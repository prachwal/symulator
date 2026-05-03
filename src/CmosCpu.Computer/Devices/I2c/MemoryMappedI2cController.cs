using NLog;

namespace CmosCpu.Computer.Devices.I2c;

public sealed class MemoryMappedI2cController
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly I2cBus _bus;
    private byte _address;
    private byte _data;
    private byte _status;

    public const byte CommandWriteByte = 0x04;

    public MemoryMappedI2cController(I2cBus bus)
    {
        _bus = bus;
    }

    public byte Read(ushort offset)
    {
        byte value = offset switch
        {
            0 => _status,
            1 => _address,
            2 => _data,
            3 => _status,
            _ => 0xFF
        };
        Logger.Trace("I2C controller read offset={Offset} value=0x{Value:X2}", offset, value);
        return value;
    }

    public void Write(ushort offset, byte value)
    {
        Logger.Trace("I2C controller write offset={Offset} value=0x{Value:X2}", offset, value);
        switch (offset)
        {
            case 0: ExecuteCommand(value); break;
            case 1: _address = value; break;
            case 2: _data = value; break;
            case 3: _status = value; break;
        }
    }

    private void ExecuteCommand(byte command)
    {
        switch (command)
        {
            case CommandWriteByte:
                bool ack = _bus.WriteByte(_address, _data);
                _status = ack ? (byte)0x00 : (byte)0x01;
                Logger.Debug("I2C WRITE addr=0x{Addr:X2} data=0x{Data:X2} ack={Ack}", _address, _data, ack);
                break;
            default:
                _status = 0x80;
                Logger.Warn("I2C unknown command: 0x{Command:X2}", command);
                break;
        }
    }
}
