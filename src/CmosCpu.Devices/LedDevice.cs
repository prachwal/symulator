using CmosCpu.Core;

using NLog;

namespace CmosCpu.Devices;

public class LedDevice : IBusDevice
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private byte _state;

    public ushort StartAddress => 0xC000;
    public ushort EndAddress => 0xC000;

    public bool IsOn => _state == 0x01;

    public event EventHandler<LedStateChangedEventArgs>? StateChanged;

    public bool Contains(ushort address)
    {
        return address == StartAddress;
    }

    public byte Read(ushort address)
    {
        return _state;
    }

    public void Write(ushort address, byte value)
    {
        byte oldState = _state;
        _state = value == 0 ? (byte)0 : (byte)1;
        if (oldState != _state)
        {
            Logger.Info("LED {State}", _state == 1 ? "ON" : "OFF");
            StateChanged?.Invoke(this, new LedStateChangedEventArgs { IsOn = IsOn });
        }
    }
}