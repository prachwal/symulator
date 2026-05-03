namespace CmosCpu.Computer.Abstractions;

public interface IMemoryMappedDevice
{
    ushort StartAddress { get; }
    ushort EndAddress { get; }
    byte Read(ushort address);
    void Write(ushort address, byte value);
}
