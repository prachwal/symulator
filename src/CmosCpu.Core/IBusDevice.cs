namespace CmosCpu.Core;

public interface IBusDevice
{
    ushort StartAddress { get; }
    ushort EndAddress { get; }
    bool Contains(ushort address);
    byte Read(ushort address);
    void Write(ushort address, byte value);
}