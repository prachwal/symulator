using NLog;

namespace CmosCpu.Computer.Devices;

/// <summary>Clean HD44780 LCD controller core.</summary>
public sealed class Hd44780Lcd
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const int DdramSize = 0x80;
    public const int CgramSize = 0x40;
    public const int VisibleCols = 16;
    public const int VisibleRows = 2;

    private const uint DefaultClockHz = 270_000; // HD44780 internal clock
    private const uint MicrosLong = 1520;  // Clear/Home
    private const uint MicrosShort = 37;   // Most commands

    private readonly byte[] _ddram = new byte[DdramSize];
    private readonly byte[] _cgram = new byte[CgramSize];
    private byte _addressCounter;
    private bool _cgramMode;
    private bool _eightBitMode = true;
    private bool _twoLineMode = true;
    private bool _fiveByTenMode;
    private bool _displayOn;
    private bool _cursorOn;
    private bool _blinkOn;
    private bool _increment = true;
    private bool _shiftOnWrite;
    private ulong _currentCycle;
    private ulong _busyUntilCycle;
    private bool _readPending;
    private byte _readLatch;
    private readonly uint _clockHz;

    public event Action<Hd44780Lcd>? DisplayChanged;

    public Hd44780Lcd(uint clockHz = DefaultClockHz)
    {
        _clockHz = clockHz > 0 ? clockHz : DefaultClockHz;
        Reset();
    }

    public void Reset()
    {
        Array.Clear(_ddram);
        Array.Clear(_cgram);
        _addressCounter = 0;
        _cgramMode = false;
        _eightBitMode = true;
        _twoLineMode = true;
        _fiveByTenMode = false;
        _displayOn = false;
        _cursorOn = false;
        _blinkOn = false;
        _increment = true;
        _shiftOnWrite = false;
        _busyUntilCycle = 0;
        _readPending = false;
        _readLatch = 0;
        Logger.Debug("HD44780 Reset");
    }

    public void Tick(ulong cycle) => _currentCycle = cycle;

    public bool IsBusy => _currentCycle < _busyUntilCycle;

    public void ExecuteInstruction(byte cmd)
    {
        if (IsBusy)
        {
            Logger.Debug("HD44780 ignored instruction while busy: 0x{Value:X2}", cmd);
            return;
        }

        if ((cmd & 0x80) != 0)
        {
            _addressCounter = (byte)(cmd & 0x7F);
            _cgramMode = false;
            _readPending = true;
            _readLatch = _ddram[_addressCounter & 0x7F];
            SetBusyMicros(MicrosShort);
            FireChanged("SetDDRAM");
            return;
        }
        if ((cmd & 0x40) != 0)
        {
            _addressCounter = (byte)(cmd & 0x3F);
            _cgramMode = true;
            _readPending = true;
            _readLatch = _cgram[_addressCounter & 0x3F];
            SetBusyMicros(MicrosShort);
            Logger.Trace("HD44780 SetCGRAM address=0x{Addr:X2}", _addressCounter);
            return;
        }
        if ((cmd & 0x20) != 0)
        {
            _eightBitMode = (cmd & 0x10) != 0;
            _twoLineMode = (cmd & 0x08) != 0;
            _fiveByTenMode = (cmd & 0x04) != 0;
            SetBusyMicros(MicrosShort);
            Logger.Debug("HD44780 FunctionSet 8bit={0} 2line={1} 5x10={2}", _eightBitMode, _twoLineMode, _fiveByTenMode);
            return;
        }
        if ((cmd & 0x10) != 0)
        {
            Shift(cmd);
            SetBusyMicros(MicrosShort);
            FireChanged("Shift");
            return;
        }
        if ((cmd & 0x08) != 0)
        {
            _displayOn = (cmd & 0x04) != 0;
            _cursorOn = (cmd & 0x02) != 0;
            _blinkOn = (cmd & 0x01) != 0;
            SetBusyMicros(MicrosShort);
            Logger.Debug("HD44780 DisplayControl display={0} cursor={1} blink={2}", _displayOn, _cursorOn, _blinkOn);
            FireChanged("DisplayControl");
            return;
        }
        if ((cmd & 0x04) != 0)
        {
            _increment = (cmd & 0x02) != 0;
            _shiftOnWrite = (cmd & 0x01) != 0;
            SetBusyMicros(MicrosShort);
            Logger.Trace("HD44780 EntryMode increment={0} shift={1}", _increment, _shiftOnWrite);
            return;
        }
        if ((cmd & 0x0F) == 0x01)
        {
            Array.Clear(_ddram);
            _addressCounter = 0;
            _cgramMode = false;
            SetBusyMicros(MicrosLong);
            Logger.Debug("HD44780 ClearDisplay");
            FireChanged("Clear");
            return;
        }
        if ((cmd & 0x0F) >= 0x02)
        {
            _addressCounter = 0;
            _cgramMode = false;
            SetBusyMicros(MicrosLong);
            Logger.Debug("HD44780 ReturnHome");
            FireChanged("Home");
            return;
        }
    }

    public void WriteData(byte value)
    {
        if (IsBusy)
        {
            Logger.Debug("HD44780 ignored data write while busy: 0x{Value:X2}", value);
            return;
        }
        if (_cgramMode)
            _cgram[_addressCounter & 0x3F] = value;
        else
            _ddram[_addressCounter & 0x7F] = value;

        Logger.Debug("HD44780 DataWrite 0x{Value:X2} '{Char}' addr=0x{Addr:X2} target={Target}",
            value, value >= 0x20 && value < 0x7F ? (char)value : '?',
            _addressCounter, _cgramMode ? "CGRAM" : "DDRAM");

        StepAddressCounter();
        SetBusyMicros(MicrosShort);
        FireChanged("Data");
    }

    public byte ReadStatus()
    {
        byte bf = IsBusy ? (byte)0x80 : (byte)0;
        byte status = (byte)(bf | (_addressCounter & 0x7F));
        Logger.Debug("HD44780 StatusRead busy={0} ac=0x{Addr:X2} status=0x{Status:X2}", IsBusy, _addressCounter, status);
        return status;
    }

    public byte ReadData()
    {
        if (_readPending)
        {
            _readPending = false;
            byte result = _readLatch;
            StepAddressCounter();
            PreloadReadLatch();
            Logger.Trace("HD44780 DataRead (pipeline) value=0x{Value:X2}", result);
            return result;
        }
        byte value;
        if (_cgramMode)
            value = _cgram[_addressCounter & 0x3F];
        else
            value = _ddram[_addressCounter & 0x7F];
        StepAddressCounter();
        Logger.Trace("HD44780 DataRead value=0x{Value:X2}", value);
        return value;
    }

    private void PreloadReadLatch()
    {
        if (_cgramMode)
            _readLatch = _cgram[_addressCounter & 0x3F];
        else
            _readLatch = _ddram[_addressCounter & 0x7F];
    }

    // Snapshot access for UI
    public byte[] Ddram => _ddram;
    public byte[] Cgram => _cgram;
    public bool DisplayOn => _displayOn;
    public bool CursorOn => _cursorOn;
    public bool BlinkOn => _blinkOn;
    public byte AddressCounter => _addressCounter;
    public int CursorRow => _addressCounter < 0x40 ? 0 : 1;
    public int CursorCol => (_addressCounter & 0x3F) % VisibleCols;
    public bool EightBitMode => _eightBitMode;
    public bool TwoLineMode => _twoLineMode;
    public ulong BusyUntilCycle => _busyUntilCycle;

    public Hd44780Snapshot GetSnapshot()
    {
        var ddram = new byte[DdramSize];
        var cgram = new byte[CgramSize];
        Array.Copy(_ddram, ddram, DdramSize);
        Array.Copy(_cgram, cgram, CgramSize);
        return new Hd44780Snapshot
        {
            Ddram = ddram,
            Cgram = cgram,
            DisplayOn = _displayOn,
            CursorOn = _cursorOn,
            BlinkOn = _blinkOn,
            AddressCounter = _addressCounter,
            Busy = IsBusy,
            EightBitMode = _eightBitMode,
            TwoLineMode = _twoLineMode,
        };
    }

    private void Shift(byte cmd)
    {
        bool shiftDisplay = (cmd & 0x08) != 0;
        bool right = (cmd & 0x04) != 0;
        if (shiftDisplay)
        {
            for (int row = 0; row < 2; row++)
            {
                int baseAddr = row == 0 ? 0 : 0x40;
                if (right)
                {
                    byte last = _ddram[baseAddr + 39];
                    for (int i = 39; i > 0; i--)
                        _ddram[baseAddr + i] = _ddram[baseAddr + i - 1];
                    _ddram[baseAddr] = last;
                }
                else
                {
                    byte first = _ddram[baseAddr];
                    for (int i = 0; i < 39; i++)
                        _ddram[baseAddr + i] = _ddram[baseAddr + i + 1];
                    _ddram[baseAddr + 39] = first;
                }
            }
        }
        else
        {
            if (right)
                _addressCounter = (byte)((_addressCounter + 1) & 0x7F);
            else
                _addressCounter = (byte)((_addressCounter - 1) & 0x7F);
        }
    }

    private void StepAddressCounter()
    {
        if (_increment)
            _addressCounter = (byte)((_addressCounter + 1) & 0x7F);
        else
            _addressCounter = (byte)((_addressCounter - 1) & 0x7F);
    }

    private void SetBusyMicros(uint microseconds)
    {
        ulong cycles = ((ulong)_clockHz * microseconds + 999_999UL) / 1_000_000UL;
        _busyUntilCycle = _currentCycle + cycles;
    }

    private void FireChanged(string reason)
    {
        Logger.Trace("HD44780 DisplayChanged reason={Reason}", reason);
        DisplayChanged?.Invoke(this);
    }
}
