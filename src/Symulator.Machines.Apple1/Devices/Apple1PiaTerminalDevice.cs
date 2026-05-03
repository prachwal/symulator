using System.Text;

namespace Symulator.Machines.Apple1.Devices;

public sealed class Apple1PiaTerminalDevice
{
    private readonly List<string> _lines = [];
    private readonly StringBuilder _currentLine = new();
    private readonly StringBuilder _outputStream = new();
    private readonly Queue<byte> _keyBuffer = [];
    private byte _lastKey;
    private bool _keyReady;
    private byte _displayControl;
    private long _version;

    public ushort StartAddress { get; } = 0xD010;
    public ushort EndAddress { get; } = 0xD013;
    public int Columns { get; }
    public int Rows { get; }
    public int OutputLength => _outputStream.Length;
    public int PendingKeyCount => _keyBuffer.Count;

    public string[] Lines
    {
        get
        {
            var allLines = new List<string>(_lines);
            string current = _currentLine.ToString();
            if (current.Length > 0)
                allLines.Add(current);
            return [.. allLines];
        }
    }
    public string Text => string.Join("\n", Lines);
    public long Version => _version;

    public string ConsumeOutputSince(int offset)
    {
        if (offset < 0)
            offset = 0;

        if (offset >= _outputStream.Length)
            return string.Empty;

        return _outputStream.ToString(offset, _outputStream.Length - offset);
    }

    public Apple1PiaTerminalDevice(int columns = 40, int rows = 24)
    {
        Columns = columns;
        Rows = rows;
        _displayControl = 0x80;
    }

    public Apple1PiaTerminalDevice(ushort startAddress, ushort endAddress, int columns = 40, int rows = 24)
    {
        StartAddress = startAddress;
        EndAddress = endAddress;
        Columns = columns;
        Rows = rows;
        _displayControl = 0x80;
    }

    public bool Handles(ushort address) => address >= StartAddress && address <= EndAddress;

    public void QueueKey(char key)
    {
        _keyBuffer.Enqueue((byte)((key & 0x7F) | 0x80));
        _keyReady = true;
    }

    public byte Read(ushort address)
    {
        return address switch
        {
            0xD010 => ReadKeyboardData(),
            0xD011 => ReadKeyboardStatus(),
            0xD012 => 0,
            0xD013 => 0x80,
            _ => 0
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case 0xD012:
                WriteDisplay(value);
                break;
            case 0xD013:
                _displayControl = value;
                break;
        }
    }

    private byte ReadKeyboardData()
    {
        if (_keyBuffer.Count > 0)
        {
            _lastKey = _keyBuffer.Dequeue();
            _keyReady = _keyBuffer.Count > 0;
        }
        return _lastKey;
    }

    private byte ReadKeyboardStatus()
    {
        return (byte)(_keyReady ? 0x80 : 0x00);
    }

    private void WriteDisplay(byte value)
    {
        char c = (char)(value & 0x7F);

        if (c == '\r' || c == '\n')
        {
            _outputStream.AppendLine();
            FlushCurrentLine();
        }
        else
        {
            _outputStream.Append(c);
            _currentLine.Append(c);
        }

        _version++;
    }

    private void FlushCurrentLine()
    {
        string line = _currentLine.ToString();
        _currentLine.Clear();
        _lines.Add(line);

        if (_lines.Count > Rows)
            _lines.RemoveAt(0);
    }
}
