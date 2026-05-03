namespace CmosCpu.Computer.Devices.I2c;

public sealed class I2cBus
{
    private readonly Dictionary<byte, II2cDevice> _devices = new();

    public void Attach(II2cDevice device)
    {
        _devices[device.Address] = device;
    }

    public bool WriteByte(byte targetAddress, byte value)
    {
        if (!_devices.TryGetValue(targetAddress, out var device))
            return false;
        device.WriteByte(value);
        return true;
    }

    public bool TryReadByte(byte targetAddress, out byte value)
    {
        if (!_devices.TryGetValue(targetAddress, out var device))
        {
            value = 0xFF;
            return false;
        }
        value = device.ReadByte();
        return true;
    }
}
