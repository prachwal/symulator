namespace CmosCpu.Computer.Devices.Serial;

public sealed class SerialByteEventArgs : EventArgs
{
    public byte Value { get; }
    public SerialByteEventArgs(byte value) => Value = value;
}
