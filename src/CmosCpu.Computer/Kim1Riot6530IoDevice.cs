namespace CmosCpu.Computer;

public sealed class Kim1Riot6530IoDevice
{
    private readonly byte[] _registers = new byte[256];
    private long _version;

    public ushort StartAddress { get; } = 0x1700;
    public ushort EndAddress { get; } = 0x17FF;
    public long Version => _version;

    public Kim1KeypadState? Keypad { get; set; }

    public bool Handles(ushort address) => address >= StartAddress && address <= EndAddress;

    public byte Read(ushort address)
    {
        int offset = address - StartAddress;
        return _registers[offset];
    }

    public void Write(ushort address, byte value)
    {
        int offset = address - StartAddress;
        if (_registers[offset] != value)
        {
            _registers[offset] = value;
            _version++;
        }
    }

    public byte[] DumpRegisters() => _registers.ToArray();
}
