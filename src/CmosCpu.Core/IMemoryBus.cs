namespace CmosCpu.Core;

public interface IMemoryBus
{
    byte ReadByte(ushort address);
    void WriteByte(ushort address, byte value);
}
