using System.Text;

namespace CmosCpu.Computer;

public sealed class Apple1PiaTerminalDevice
{
    private readonly List<string> _lines = [];
    private readonly StringBuilder _currentLine = new();
    private byte _lastKey;
    private bool _keyReady;
    private byte _displayControl;
    private long _version;
    private int _lastConsumedLineIndex;
    private long _lastConsumedVersion;

    public ushort StartAddress { get; } = 0xD010;
    public ushort EndAddress { get; } = 0xD013;
    public int Columns { get; }
    public int Rows { get; }

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

    public string ConsumeOutputSince(long version)
    {
        if (_version <= version)
            return string.Empty;

        var sb = new StringBuilder();
        int lineIndex = 0;

        foreach (var line in _lines)
        {
            if (lineIndex >= _lastConsumedLineIndex)
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.Append(line);
            }
            lineIndex++;
        }

        // Include the current (in-progress) line if it has content
        string current = _currentLine.ToString();
        if (current.Length > 0)
        {
            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append(current);
        }

        _lastConsumedLineIndex = _lines.Count;
        _lastConsumedVersion = _version;
        return sb.ToString();
    }

    public Apple1PiaTerminalDevice(int columns = 40, int rows = 24)
    {
        Columns = columns;
        Rows = rows;
    }

    public bool Handles(ushort address) => address >= StartAddress && address <= EndAddress;

    public void QueueKey(char key)
    {
        _lastKey = (byte)(key & 0x7F);
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
        _keyReady = false;
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
            FlushCurrentLine();
        }
        else
        {
            _currentLine.Append(c);
            _version++;
        }
    }

    private void FlushCurrentLine()
    {
        string line = _currentLine.ToString();
        _currentLine.Clear();
        _lines.Add(line);
        _version++;

        if (_lines.Count > Rows)
            _lines.RemoveAt(0);
    }
}
