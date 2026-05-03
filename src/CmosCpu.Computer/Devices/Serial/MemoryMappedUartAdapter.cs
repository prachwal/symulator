namespace CmosCpu.Computer.Devices.Serial;

public sealed class MemoryMappedUartAdapter
{
    private readonly UartDevice _uart;

    public MemoryMappedUartAdapter(UartDevice uart) => _uart = uart;

    public byte Read(ushort offset) => offset switch
    {
        0 => _uart.ReadData(),
        1 => _uart.ReadStatus(),
        2 => _uart.ReadControl(),
        _ => 0xFF
    };

    public void Write(ushort offset, byte value)
    {
        switch (offset)
        {
            case 0: _uart.WriteData(value); break;
            case 1: break; // Status write — noop for now
            case 2: _uart.WriteControl(value); break;
        }
    }
}
