using System.Collections.Frozen;

namespace CmosCpu.Computer;

public sealed class Kim1KeypadState
{
    private readonly HashSet<string> _pressed = [];

    public IReadOnlySet<string> PressedKeys => _pressed;

    private static readonly FrozenSet<string> ValidKeys = new HashSet<string>
    {
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
        "A", "B", "C", "D", "E", "F",
        "AD", "DA", "GO", "PC", "+"
    }.ToFrozenSet();

    private static readonly FrozenDictionary<string, (int row, int col)> KeyMatrix = new Dictionary<string, (int, int)>
    {
        ["0"] = (0, 0), ["1"] = (0, 1), ["2"] = (0, 2), ["3"] = (0, 3),
        ["4"] = (1, 0), ["5"] = (1, 1), ["6"] = (1, 2), ["7"] = (1, 3),
        ["8"] = (2, 0), ["9"] = (2, 1), ["A"] = (2, 2), ["B"] = (2, 3),
        ["C"] = (3, 0), ["D"] = (3, 1), ["E"] = (3, 2), ["F"] = (3, 3),
        ["AD"] = (4, 0), ["DA"] = (4, 1), ["GO"] = (4, 2), ["PC"] = (4, 3),
        ["+"] = (5, 0)
    }.ToFrozenDictionary();

    public bool IsValidKey(string key) => ValidKeys.Contains(key);

    public void PressKey(string key)
    {
        if (ValidKeys.Contains(key))
            _pressed.Add(key);
    }

    public void ReleaseKey(string key)
    {
        _pressed.Remove(key);
    }

    public (int row, int col) GetKeyMatrix(string key) =>
        KeyMatrix.TryGetValue(key, out var pos) ? pos : (-1, -1);

    public byte GetRowState(byte columnOutput, byte columnDdr)
    {
        byte rowBits = 0x0F;
        bool anyColumnActive = false;
        for (int c = 0; c < 4; c++)
        {
            bool ddrIsOutput = (columnDdr & (1 << c)) != 0;
            bool outputIsLow = (columnOutput & (1 << c)) == 0;
            if (ddrIsOutput && outputIsLow)
            {
                anyColumnActive = true;
                foreach (string key in _pressed)
                {
                    if (!KeyMatrix.TryGetValue(key, out var pos))
                        continue;
                    if (pos.col == c)
                        rowBits &= (byte)~(1 << pos.row);
                }
            }
        }

        return anyColumnActive ? rowBits : (byte)0x0F;
    }
}
