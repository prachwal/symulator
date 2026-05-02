using NLog;

namespace CmosCpu.Computer;

public sealed class Kim1Riot6530IoDevice
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private byte _portAData;
    private byte _portADdr;
    private byte _portBData;
    private byte _portBDdr;
    private byte _portAInput;
    private byte _portBInput;
    private readonly byte[] _internalRam = new byte[128];

    private int _timerCounter;
    private int _timerPrescalerDivider;
    private int _timerPrescalerCounter;
    private bool _timerUnderflow;
    private bool _timerIrqEnabled;
    private bool _timerIrqPending;

    private byte _prevPa7Input;
    private bool _pa7IrqPending;

    private ushort _startAddress;
    private ushort _endAddress;
    private long _version;

    public ushort StartAddress => _startAddress;
    public ushort EndAddress => _endAddress;
    public long Version => _version;

    public bool IrqPending => (_timerIrqPending && _timerIrqEnabled) || _pa7IrqPending;
    public int TimerValue => _timerCounter;
    public int TimerPrescalerDivider => _timerPrescalerDivider;
    public bool TimerUnderflow => _timerUnderflow;
    public byte PortAData => _portAData;
    public byte PortADdr => _portADdr;
    public byte PortBData => _portBData;
    public byte PortBDdr => _portBDdr;
    public byte PortAInputValue => _portAInput;
    public byte PortBInputValue => _portBInput;
    public bool TimerIrqEnabled => _timerIrqEnabled;
    public bool TimerIrqPendingRaw => _timerIrqPending;
    public bool Pa7IrqPending => _pa7IrqPending;
    public byte[] InternalRam => _internalRam;

    public Kim1KeypadState? Keypad { get; set; }

    public Kim1Riot6530IoDevice(ushort startAddress = 0x1700, ushort endAddress = 0x17FF)
    {
        _startAddress = startAddress;
        _endAddress = endAddress;
    }

    public bool Handles(ushort address) => address >= _startAddress && address <= _endAddress;

    public byte Read(ushort address)
    {
        int offset = address - _startAddress;
        offset &= 0xFF;

        if (offset >= 0x40 && offset < 0xC0)
            return _internalRam[offset - 0x40];

        if ((offset & 0xC0) == 0xC0)
            offset &= 0x3F;

        switch (offset)
        {
            case 0x00:
                return CombinePortWithInput(_portAData, _portADdr, _portAInput);
            case 0x01:
                return _portADdr;
            case 0x02:
                return CombinePortWithInput(_portBData, _portBDdr, _portBInput);
            case 0x03:
                return _portBDdr;
            case 0x04:
                return (byte)(_timerCounter & 0xFF);
            case 0x05:
            case 0x06:
                {
                    int highBits = (_timerCounter >> 8) & 0x7F;
                    byte flags = (byte)(_timerUnderflow ? 0x80 : 0x00);
                    ClearTimerFlags();
                    return (byte)(highBits | flags);
                }
            default:
                return 0;
        }
    }

    public void Write(ushort address, byte value)
    {
        int offset = address - _startAddress;
        offset &= 0xFF;

        if (offset >= 0x40 && offset < 0xC0)
        {
            if (_internalRam[offset - 0x40] != value)
            {
                _internalRam[offset - 0x40] = value;
                _version++;
            }
            return;
        }

        if ((offset & 0xC0) == 0xC0)
            offset &= 0x3F;

        switch (offset)
        {
            case 0x00:
                if (_portAData != value) { _portAData = value; _version++; }
                break;
            case 0x01:
                if (_portADdr != value) { _portADdr = value; _version++; }
                break;
            case 0x02:
                if (_portBData != value) { _portBData = value; _version++; }
                break;
            case 0x03:
                if (_portBDdr != value) { _portBDdr = value; _version++; }
                break;
            case 0x04:
                LoadTimer(value, 1);
                break;
            case 0x05:
                LoadTimer(value, 8);
                break;
            case 0x06:
                LoadTimer(value, 64);
                break;
            case 0x07:
                LoadTimer(value, 1024);
                break;
            case 0x08:
                LoadTimer(value, 1);
                _timerIrqEnabled = true;
                break;
            case 0x09:
                _timerIrqEnabled = false;
                break;
            default:
                break;
        }
    }

    public void Tick(int cpuCycles)
    {
        if (_timerCounter == 0 && _timerUnderflow)
            return;

        for (int i = 0; i < cpuCycles; i++)
        {
            _timerPrescalerCounter++;
            if (_timerPrescalerCounter >= _timerPrescalerDivider)
            {
                _timerPrescalerCounter = 0;
                if (_timerCounter > 0)
                {
                    _timerCounter--;
                    if (_timerCounter == 0)
                    {
                        _timerUnderflow = true;
                        if (_timerIrqEnabled)
                            _timerIrqPending = true;
                    }
                }
            }
        }
    }

    public void SetPortAInput(byte value)
    {
        byte newPa7 = (byte)((value >> 7) & 1);
        byte oldPa7 = (byte)((_portAInput >> 7) & 1);

        if (oldPa7 == 1 && newPa7 == 0)
        {
            _pa7IrqPending = true;
        }

        _portAInput = value;
    }

    public void SetPortBInput(byte value)
    {
        _portBInput = value;
    }

    public void ClearIrq()
    {
        _timerIrqPending = false;
        _pa7IrqPending = false;
    }

    public byte[] DumpRegisters()
    {
        var data = new byte[256];
        data[0x00] = _portAData;
        data[0x01] = _portADdr;
        data[0x02] = _portBData;
        data[0x03] = _portBDdr;
        data[0x04] = (byte)(_timerCounter & 0xFF);
        data[0x05] = (byte)(((_timerCounter >> 8) & 0x7F) | (_timerUnderflow ? 0x80 : 0x00));
        Array.Copy(_internalRam, 0, data, 0x40, 128);
        return data;
    }

    private byte CombinePortWithInput(byte latch, byte ddr, byte input)
    {
        byte outputBits = (byte)(latch & ddr);
        byte inputBits = (byte)(input & ~ddr);
        return (byte)(outputBits | inputBits);
    }

    private void LoadTimer(byte value, int prescalerDivider)
    {
        _timerCounter = value;
        _timerPrescalerDivider = prescalerDivider;
        _timerPrescalerCounter = 0;
        _timerUnderflow = false;
        _timerIrqPending = false;
    }

    private void ClearTimerFlags()
    {
        _timerUnderflow = false;
        _timerIrqPending = false;
    }
}
