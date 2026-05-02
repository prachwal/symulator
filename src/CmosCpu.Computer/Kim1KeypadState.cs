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
}
