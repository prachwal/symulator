namespace CmosCpu.Computer.Devices;

public sealed class Hd44780Snapshot
{
    public byte[] Ddram { get; init; } = [];
    public byte[] Cgram { get; init; } = [];
    public bool DisplayOn { get; init; }
    public bool CursorOn { get; init; }
    public bool BlinkOn { get; init; }
    public byte AddressCounter { get; init; }
    public bool Busy { get; init; }
    public bool EightBitMode { get; init; }
    public bool TwoLineMode { get; init; }
}
