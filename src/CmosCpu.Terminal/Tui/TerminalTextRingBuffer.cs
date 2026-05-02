using System.Text;

namespace CmosCpu.Terminal.Tui;

public sealed class TerminalTextRingBuffer
{
    private readonly char[] _buffer;
    private int _start;
    private int _end;
    private int _length;

    public int MaxChars { get; }
    public int Length => _length;

    public TerminalTextRingBuffer(int maxChars = 65536)
    {
        MaxChars = maxChars > 0 ? maxChars : 65536;
        _buffer = new char[MaxChars];
    }

    public void Append(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        foreach (char c in text)
        {
            if (_length < MaxChars)
            {
                _buffer[_end] = c;
                _end = (_end + 1) % MaxChars;
                _length++;
            }
            else
            {
                _buffer[_end] = c;
                _start = (_start + 1) % MaxChars;
                _end = (_end + 1) % MaxChars;
            }
        }
    }

    public void Clear()
    {
        _start = 0;
        _end = 0;
        _length = 0;
    }

    public string GetText()
    {
        if (_length == 0)
            return string.Empty;

        var sb = new StringBuilder(_length);
        int idx = _start;
        for (int i = 0; i < _length; i++)
        {
            sb.Append(_buffer[idx]);
            idx = (idx + 1) % MaxChars;
        }
        return sb.ToString();
    }
}
