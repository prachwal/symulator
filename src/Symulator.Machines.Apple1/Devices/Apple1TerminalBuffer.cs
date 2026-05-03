using System.Text;

namespace Symulator.Machines.Apple1.Devices;

public sealed class Apple1TerminalBuffer
{
    private readonly char[,] _cells;
    private readonly StringBuilder _outputStream = new();
    private int _cursorRow;
    private int _cursorCol;
    private long _version;

    public int Columns { get; }
    public int Rows { get; }
    public char[,] Cells => _cells;
    public int CursorRow => _cursorRow;
    public int CursorColumn => _cursorCol;
    public long Version => _version;
    public string Text => GetText();
    public string OutputStream => _outputStream.ToString();

    public Apple1TerminalBuffer(int columns = 40, int rows = 24)
    {
        Columns = columns;
        Rows = rows;
        _cells = new char[rows, columns];
        Clear();
    }

    public void Clear()
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
                _cells[r, c] = ' ';
        _cursorRow = 0;
        _cursorCol = 0;
        _outputStream.Clear();
        _version++;
    }

    public void Write(byte ascii)
    {
        byte ch = (byte)(ascii & 0x7F);

        if (ch == 0x0D)
        {
            _cursorCol = 0;
            if (_cursorRow < Rows - 1)
                _cursorRow++;
            _outputStream.Append('\n');
            _version++;
            return;
        }

        if (ch < 0x20)
            return;

        _cells[_cursorRow, _cursorCol] = (char)ch;
        _cursorCol++;
        _outputStream.Append((char)ch);

        if (_cursorCol >= Columns)
        {
            _cursorCol = 0;
            if (_cursorRow >= Rows - 1)
                Scroll();
            else
                _cursorRow++;
        }

        _version++;
    }

    private void Scroll()
    {
        for (int r = 1; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
                _cells[r - 1, c] = _cells[r, c];

        for (int c = 0; c < Columns; c++)
            _cells[Rows - 1, c] = ' ';
    }

    private string GetText()
    {
        var lines = new List<string>();
        for (int r = 0; r < Rows; r++)
        {
            var line = new char[Columns];
            for (int c = 0; c < Columns; c++)
                line[c] = _cells[r, c];
            lines.Add(new string(line).TrimEnd());
        }
        return string.Join("\n", lines);
    }
}
