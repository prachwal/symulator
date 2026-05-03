namespace Symulator.Machines.Kim1.Devices;

public sealed class Kim1KeypadState
{
    private readonly Dictionary<string, bool> _keys = [];
    private static readonly Dictionary<string, (int Row, int Col)> Kim1KeyPositions = new()
    {
        ["D7"] = (0, 0), ["D8"] = (1, 0), ["D9"] = (2, 0), ["A"] = (3, 0),
        ["D4"] = (0, 1), ["D5"] = (1, 1), ["D6"] = (2, 1), ["B"] = (3, 1),
        ["D1"] = (0, 2), ["D2"] = (1, 2), ["D3"] = (2, 2), ["C"] = (3, 2),
        ["D0"] = (0, 3), ["F"] = (1, 3), ["E"] = (2, 3), ["D"] = (3, 3),
    };

    public void PressKey(string key)
    {
        _keys[key] = true;
    }

    public void ReleaseKey(string key)
    {
        _keys[key] = false;
    }

    public byte GetRowState(byte columnOutput, byte columnDdr)
    {
        byte rowResult = 0;

        foreach (var kvp in _keys)
        {
            if (kvp.Value && Kim1KeyPositions.TryGetValue(kvp.Key, out var pos))
            {
                byte colBit = (byte)(1 << pos.Col);
                if ((columnOutput & columnDdr & colBit) == colBit)
                {
                    rowResult |= (byte)(1 << pos.Row);
                }
            }
        }

        return rowResult;
    }

    public void Reset()
    {
        _keys.Clear();
    }
}
