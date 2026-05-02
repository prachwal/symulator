namespace CmosCpu.Core;

public class MemoryCellSnapshot
{
    public ushort Address { get; init; }
    public byte Value { get; init; }
    public bool IsPC { get; init; }
    public bool IsWritten { get; init; }
}