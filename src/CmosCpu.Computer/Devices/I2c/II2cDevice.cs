namespace CmosCpu.Computer.Devices.I2c;

public interface II2cDevice
{
    byte Address { get; }
    void WriteByte(byte value);
    byte ReadByte();
}
