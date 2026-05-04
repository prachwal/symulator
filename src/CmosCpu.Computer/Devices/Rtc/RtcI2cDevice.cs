using CmosCpu.Computer.Devices.I2c;

namespace CmosCpu.Computer.Devices.Rtc;

public sealed class RtcI2cDevice : II2cDevice
{
    private readonly RtcClockCore _clock;
    private byte _registerPointer;
    private bool _expectingRegisterPointer = true;

    public RtcI2cDevice(RtcClockCore clock, byte address = 0x68)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Address = address;
    }

    public byte Address { get; }

    public RtcClockCore Clock => _clock;

    public byte RegisterPointer => _registerPointer;

    public void BeginWrite()
    {
        _expectingRegisterPointer = true;
    }

    public void WriteByte(byte value)
    {
        if (_expectingRegisterPointer)
        {
            _registerPointer = NormalizeRegister(value);
            _expectingRegisterPointer = false;
            return;
        }

        _clock.WriteRegister(_registerPointer, value);
        IncrementPointer();
    }

    public void WriteBytes(params byte[] values)
    {
        BeginWrite();
        foreach (var value in values)
            WriteByte(value);
    }

    public byte ReadByte()
    {
        var value = _clock.ReadRegister(_registerPointer);
        IncrementPointer();
        return value;
    }

    public byte[] ReadBytes(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        var buffer = new byte[count];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = ReadByte();
        return buffer;
    }

    public RtcSnapshot CreateSnapshot() => _clock.CreateSnapshot(i2cAddress: Address);

    private void IncrementPointer() => _registerPointer = NormalizeRegister((byte)(_registerPointer + 1));

    private static byte NormalizeRegister(byte value) => (byte)(value % RtcClockCore.RegisterCount);
}
