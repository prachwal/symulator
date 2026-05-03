using NLog;

namespace Symulator.Application.Terminal;

public sealed class TerminalSnapshot
{
    public required IReadOnlyList<string> Lines { get; init; }
    public required string CurrentLine { get; init; }
}

public sealed class TerminalBuffer
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<string> _lines = new();
    private readonly object _sync = new();
    private readonly int _maxLines;
    private string _currentLine = string.Empty;

    public TerminalBuffer(int maxLines = 1000)
    {
        _maxLines = maxLines;
    }

    public void WriteByte(byte value)
    {
        lock (_sync)
        {
            switch (value)
            {
                case 0x0D:
                    return;
                case 0x0A:
                    CommitLine();
                    return;
                case 0x08:
                    Backspace();
                    return;
                default:
                    char ch = value >= 0x20 && value <= 0x7E ? (char)value : '.';
                    _currentLine += ch;
                    return;
            }
        }
    }

    public void WriteString(string text)
    {
        foreach (char ch in text)
            WriteByte((byte)ch);
    }

    public TerminalSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            var visibleLines = _lines
                .Concat(new[] { _currentLine })
                .TakeLast(200)
                .ToArray();

            return new TerminalSnapshot
            {
                Lines = visibleLines,
                CurrentLine = _currentLine
            };
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _lines.Clear();
            _currentLine = string.Empty;
            Logger.Debug("Terminal buffer cleared");
        }
    }

    private void CommitLine()
    {
        Logger.Debug("Terminal line committed: '{Line}' (total={Count})", _currentLine, _lines.Count + 1);
        _lines.Add(_currentLine);
        _currentLine = string.Empty;
        while (_lines.Count > _maxLines)
            _lines.RemoveAt(0);
    }

    private void Backspace()
    {
        if (_currentLine.Length > 0)
        {
            _currentLine = _currentLine[..^1];
            Logger.Trace("Terminal backspace, line='{Line}'", _currentLine);
        }
    }
}
