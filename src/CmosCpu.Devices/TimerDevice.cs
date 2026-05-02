using CmosCpu.Core;

using NLog;

namespace CmosCpu.Devices;

public class TimerDevice : IBusDevice
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private ushort _counter;
    private byte _control;
    private byte _status;
    private ushort _reloadValue;
    private bool _running;

    public ushort StartAddress => 0xC010;
    public ushort EndAddress => 0xC012;

    public event EventHandler? TimerFired;
    public bool IrqEnabled => (_control & 0x01) != 0;

    public bool Contains(ushort address)
    {
        return address >= StartAddress && address <= EndAddress;
    }

    public byte Read(ushort address)
    {
        return address switch
        {
            0xC010 => (byte)(_counter & 0xFF),
            0xC011 => _control,
            0xC012 => _status,
            _ => 0,
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case 0xC010:
                _reloadValue = (ushort)((_reloadValue & 0xFF00) | value);
                _counter = _reloadValue;
                break;
            case 0xC011:
                _control = value;
                _running = (_control & 0x80) != 0;
                if (_running)
                    _counter = _reloadValue;
                Logger.Debug("Timer control={Control:X2} running={Running}", _control, _running);
                break;
            case 0xC012:
                _status = value;
                break;
        }
    }

    public void Tick()
    {
        if (!_running) return;
        if (_counter > 0)
        {
            _counter--;
        }
        if (_counter == 0)
        {
            _status = 1;
            Logger.Debug("Timer fired!");
            TimerFired?.Invoke(this, EventArgs.Empty);
            if (_running)
                _counter = _reloadValue;
        }
    }
}