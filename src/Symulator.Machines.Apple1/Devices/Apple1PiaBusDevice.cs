using CmosCpu.Computer.Abstractions;
using CmosCpu.Computer.Devices;

namespace Symulator.Machines.Apple1.Devices;

public sealed class Apple1PiaBusDevice : IMemoryMappedDevice
{
    private readonly Pia6821 _pia;

    public ushort StartAddress => _pia.StartAddress;
    public ushort EndAddress => _pia.EndAddress;

    public Apple1PiaBusDevice(Pia6821 pia)
    {
        _pia = pia ?? throw new ArgumentNullException(nameof(pia));
    }

    public byte Read(ushort address)
    {
        return address switch
        {
            0xD010 => _pia.Read(address),
            0xD011 => _pia.Read(address),
            0xD012 => 0x00,
            0xD013 => _pia.Read(address),
            _ => 0x00
        };
    }

    public void Write(ushort address, byte value)
    {
        _pia.Write(address, value);
    }
}
