namespace CmosCpu.Memory;

public class RamDevice : MemoryBlock
{
    public RamDevice(ushort startAddress, ushort endAddress)
        : base(startAddress, endAddress)
    {
    }

    public override byte Read(ushort address)
    {
        return Data[ToOffset(address)];
    }

    public override void Write(ushort address, byte value)
    {
        Data[ToOffset(address)] = value;
    }
}
