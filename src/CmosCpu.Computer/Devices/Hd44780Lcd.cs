using CmosCpu.Computer.Abstractions;
using CmosCpu.Core;
using NLog;

namespace CmosCpu.Computer.Devices;

public sealed class Hd44780Lcd : IMemoryMappedDevice, IClockedDevice
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Geometry
    public const int DdramSize = 0x80;
    public const int CgramSize = 0x40;
    public const int VisibleCols = 16;
    public const int VisibleRows = 2;
    public const int CharWidth = 5;
    public const int CharHeight = 7;

    // Timing in CPU cycles (approximate: 1 µs ≈ 1 cycle at 1 MHz)
    private const long TimingLong = 1520;    // Clear/Home: 1.52 ms
    private const long TimingShort = 37;     // Most commands: 37 µs

    // Registers
    private readonly byte[] _ddram = new byte[DdramSize];
    private readonly byte[] _cgram = new byte[CgramSize];
    private byte _addressCounter;
    private bool _cgramMode;

    // Configuration (from Function Set instruction)
    private bool _eightBitMode = true;
    private bool _twoLineMode = true;
    private bool _fiveByTenMode;

    // Display control
    private bool _displayOn;
    private bool _cursorOn;
    private bool _blinkOn;

    // Entry mode
    private bool _increment = true;
    private bool _shiftOnWrite;

    // Busy timing
    private long _busyUntilCycle;

    // Read pipeline: after Set Address, first read returns old data
    private bool _readPending;
    private byte _readLatch;

    public ushort StartAddress { get; }
    public ushort EndAddress => (ushort)(StartAddress + 1);
    public long BusyUntilCycle => _busyUntilCycle;
    public bool IsBusy => false; // Tick clears it; CPU-side check via status bit

    // Observable state
    public event Action<Hd44780Lcd>? DisplayChanged;

    // Snapshot for render pipeline
    public byte[] Ddram => _ddram;
    public byte[] Cgram => _cgram;
    public bool DisplayOn => _displayOn;
    public bool CursorOn => _cursorOn;
    public bool BlinkOn => _blinkOn;
    public byte CursorAddr => _addressCounter;
    public int CursorRow => _addressCounter < 0x40 ? 0 : 1;
    public int CursorCol => (_addressCounter & 0x3F) % VisibleCols;

    public Hd44780Lcd(ushort baseAddress = 0xD020)
    {
        StartAddress = baseAddress;
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
    }

    public void Tick(ulong cycle)
    {
        // Tick is called by ComputerMachine each step; we track busy via cycle count
    }

    public byte Read(ushort address)
    {
        bool isData = (address & 1) == 1;

        if (isData) // RS=1, R/W=1 → Data register read
        {
            if (_readPending)
            {
                _readPending = false;
                return _readLatch;
            }

            byte value;
            if (_cgramMode)
                value = _cgram[_addressCounter & 0x3F];
            else
                value = _ddram[_addressCounter & 0x7F];

            StepAddressCounter();
            return value;
        }
        else // RS=0, R/W=1 → Status register read
        {
            byte bf = _busyUntilCycle > Environment.TickCount64 ? (byte)0x80 : (byte)0;
            return (byte)(bf | (_addressCounter & 0x7F));
        }
    }

    public void Write(ushort address, byte value)
    {
        if (_busyUntilCycle > Environment.TickCount64)
            return; // Silently ignore while busy

        bool isData = (address & 1) == 1;

        if (isData) // RS=1, R/W=0 → Data register write
        {
            if (_cgramMode)
                _cgram[_addressCounter & 0x3F] = value;
            else
                _ddram[_addressCounter & 0x7F] = value;

            StepAddressCounter();
            SetBusy(TimingShort);
            FireDisplayChanged();
        }
        else // RS=0, R/W=0 → Command register write
        {
            ExecuteCommand(value);
        }
    }

    private void ExecuteCommand(byte cmd)
    {
        if ((cmd & 0x80) != 0)
        {
            // Set DDRAM address
            _addressCounter = (byte)(cmd & 0x7F);
            _cgramMode = false;
            _readPending = true;
            _readLatch = _ddram[_addressCounter & 0x7F];
            SetBusy(TimingShort);
            FireDisplayChanged();
            return;
        }

        if ((cmd & 0x40) != 0)
        {
            // Set CGRAM address
            _addressCounter = (byte)(cmd & 0x3F);
            _cgramMode = true;
            _readPending = true;
            _readLatch = _cgram[_addressCounter & 0x3F];
            SetBusy(TimingShort);
            return;
        }

        // HD44780 instruction decoder (after checking DDRAM/CGRAM address)
        if ((cmd & 0x20) != 0)
        {
            // Function Set: bits 5=1, 4=DL, 3=N, 2=F
            _eightBitMode = (cmd & 0x10) != 0;
            _twoLineMode = (cmd & 0x08) != 0;
            _fiveByTenMode = (cmd & 0x04) != 0;
            SetBusy(TimingShort);
            Logger.Debug("HD44780 Function Set: 8bit={0}, 2line={1}, 5x10={2}",
                _eightBitMode, _twoLineMode, _fiveByTenMode);
            return;
        }

        if ((cmd & 0x10) != 0)
        {
            // bit4=1: Cursor/Display Shift
            Shift(cmd);
            SetBusy(TimingShort);
            FireDisplayChanged();
            return;
        }

        if ((cmd & 0x08) != 0)
        {
            // bit3=1: Display On/Off Control (0x08-0x0F)
            _displayOn = (cmd & 0x04) != 0;
            _cursorOn = (cmd & 0x02) != 0;
            _blinkOn = (cmd & 0x01) != 0;
            SetBusy(TimingShort);
            FireDisplayChanged();
            return;
        }

        if ((cmd & 0x04) != 0)
        {
            // bit2=1: Entry Mode Set (0x04-0x07)
            _increment = (cmd & 0x02) != 0;
            _shiftOnWrite = (cmd & 0x01) != 0;
            SetBusy(TimingShort);
            return;
        }

        // Clear Display (0x01) or Cursor Home (0x02-0x03)
        if ((cmd & 0x0F) == 0x01)
        {
            // Clear Display
            Array.Clear(_ddram);
            _addressCounter = 0;
            _cgramMode = false;
            SetBusy(TimingLong);
            FireDisplayChanged();
            return;
        }

        if ((cmd & 0x0F) >= 0x02)
        {
            // Cursor Home
            _addressCounter = 0;
            _cgramMode = false;
            SetBusy(TimingLong);
            FireDisplayChanged();
        }
    }

    private void Shift(byte cmd)
    {
        bool shiftDisplay = (cmd & 0x08) != 0;
        bool right = (cmd & 0x04) != 0;

        if (shiftDisplay)
        {
            // Scroll display content; DDRAM unchanged, view offset changes
            // For simplicity, we rotate DDRAM lines
            // Real HD44780 uses a display offset register — we emulate by shifting DDRAM
            if (right)
            {
                for (int row = 0; row < 2; row++)
                {
                    int baseAddr = row == 0 ? 0 : 0x40;
                    byte last = _ddram[baseAddr + 39];
                    for (int i = 39; i > 0; i--)
                        _ddram[baseAddr + i] = _ddram[baseAddr + i - 1];
                    _ddram[baseAddr] = last;
                }
            }
            else
            {
                for (int row = 0; row < 2; row++)
                {
                    int baseAddr = row == 0 ? 0 : 0x40;
                    byte first = _ddram[baseAddr];
                    for (int i = 0; i < 39; i++)
                        _ddram[baseAddr + i] = _ddram[baseAddr + i + 1];
                    _ddram[baseAddr + 39] = first;
                }
            }
        }
        else
        {
            // Move cursor
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

    private void SetBusy(long durationCycles)
    {
        _busyUntilCycle = Environment.TickCount64 + durationCycles / 1000;
    }

    private void FireDisplayChanged()
    {
        DisplayChanged?.Invoke(this);
    }
}
