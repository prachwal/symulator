using CmosCpu.Computer.Devices;
using NLog;

namespace Symulator.Machines.Apple1.Devices;

public sealed class Apple1PiaWiring
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Pia6821 _pia;
    private readonly Apple1TerminalBuffer _buffer;
    private byte _pendingKey;

    public Apple1PiaWiring(Pia6821 pia, Apple1TerminalBuffer buffer)
    {
        _pia = pia;
        _buffer = buffer;

        ConfigurePorts();

        _pia.PortBOutputChanged += OnPortBOutputChanged;
        _pia.OnPortARead += ClearPendingKey;

        UpdateKeyboardInput();
    }

    public bool HasPendingKey => _pendingKey != 0;

    public void Reset()
    {
        _pia.Reset();
        _pendingKey = 0;
        ConfigurePorts();
        UpdateKeyboardInput();
        _buffer.Clear();
        Logger.Debug("Apple-1 PIA wiring reset");
    }

    private void ConfigurePorts()
    {
        ushort craAddr = (ushort)(_pia.StartAddress + 1);
        ushort crbAddr = (ushort)(_pia.StartAddress + 3);
        ushort portBAddr = (ushort)(_pia.StartAddress + 2);

        _pia.Write(craAddr, 0x04);   // CRA bit2=1 → data mode
        _pia.Write(crbAddr, 0x00);   // CRB bit2=0 → DDR
        _pia.Write(portBAddr, 0x7F); // DDRB = 0x7F (bits 0-6 output, bit7 input for display-ready)
        _pia.PortBInput = 0x00;      // bit7=0 → display always ready
        _pia.Write(crbAddr, 0x04);   // CRB bit2=1 → data mode
    }

    public void QueueKey(char key)
    {
        _pendingKey = (byte)((key & 0x7F) | 0x80);
        UpdateKeyboardInput();
        _pia.SetCa1(true);
        Logger.Debug("Apple-1 QueueKey char='{Char}', key=0x{Key:X2}",
            key == '\r' ? "CR" : key.ToString(), _pendingKey);
    }

    private void OnPortBOutputChanged(byte value)
    {
        _buffer.Write(value);
        Logger.Trace("Apple-1 display output value=0x{Value:X2}, char='{Char}'", value, ToPrintable(value));
    }

    private void UpdateKeyboardInput()
    {
        _pia.PortAInput = _pendingKey;
    }

    public void ClearPendingKey()
    {
        _pendingKey = 0;
        UpdateKeyboardInput();
        Logger.Trace("Apple-1 key consumed, clearing pending key");
    }

    private static string ToPrintable(byte value)
    {
        byte ch = (byte)(value & 0x7F);
        return ch switch
        {
            0x0D => "CR",
            0x0A => "LF",
            < 0x20 => $"CTRL-{ch:X2}",
            0x7F => "DEL",
            _ => ((char)ch).ToString()
        };
    }
}
