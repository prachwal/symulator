namespace CmosCpu.Computer.Devices.Rtc;

public sealed class RtcBusMappedDevice
{
    private readonly RtcClockCore _clock;
    private byte _selectedRegister;

    public RtcBusMappedDevice(RtcClockCore clock, ushort baseAddress, RtcBusMode mode = RtcBusMode.Linear, ushort size = RtcClockCore.RegisterCount)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        BaseAddress = baseAddress;
        Mode = mode;
        Size = size == 0 ? (ushort)RtcClockCore.RegisterCount : size;
    }

    public ushort BaseAddress { get; }

    public ushort Size { get; }

    public RtcBusMode Mode { get; }

    public RtcClockCore Clock => _clock;

    public bool Handles(ushort address)
    {
        var endExclusive = BaseAddress + Size;
        return address >= BaseAddress && address < endExclusive;
    }

    public byte Read(ushort address)
    {
        if (!Handles(address))
            return 0xFF;

        var offset = (ushort)(address - BaseAddress);
        return Mode switch
        {
            RtcBusMode.Linear => _clock.ReadRegister((byte)offset),
            RtcBusMode.Indexed => offset switch
            {
                0 => _selectedRegister,
                1 => _clock.ReadRegister(_selectedRegister),
                _ => 0xFF
            },
            _ => 0xFF
        };
    }

    public void Write(ushort address, byte value)
    {
        if (!Handles(address))
            return;

        var offset = (ushort)(address - BaseAddress);
        switch (Mode)
        {
            case RtcBusMode.Linear:
                _clock.WriteRegister((byte)offset, value);
                break;
            case RtcBusMode.Indexed when offset == 0:
                _selectedRegister = (byte)(value % RtcClockCore.RegisterCount);
                break;
            case RtcBusMode.Indexed when offset == 1:
                _clock.WriteRegister(_selectedRegister, value);
                break;
        }
    }

    public RtcSnapshot CreateSnapshot() => _clock.CreateSnapshot(directBusBaseAddress: BaseAddress, directBusMode: Mode);
}
