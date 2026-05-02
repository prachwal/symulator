using NLog;

namespace CmosCpu.Memory;

public class RomDevice : MemoryBlock
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public RomDevice(ushort startAddress, ushort endAddress)
        : base(startAddress, endAddress)
    {
    }

    public override byte Read(ushort address)
    {
        return Data[ToOffset(address)];
    }

    public override void Write(ushort address, byte value)
    {
        Logger.Warn("ROM WRITE attempted at {Address:X4} - ignored (read-only)", address);
    }
}