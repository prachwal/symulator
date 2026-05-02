using System.Collections.Frozen;

namespace Symulator.Machines.Kim1.Services;

public static class Kim1KeyMapper
{
    private static readonly FrozenDictionary<string, string> KeyMap = new Dictionary<string, string>
    {
        ["D0"] = "0", ["D1"] = "1", ["D2"] = "2", ["D3"] = "3",
        ["D4"] = "4", ["D5"] = "5", ["D6"] = "6", ["D7"] = "7",
        ["D8"] = "8", ["D9"] = "9",
        ["A"] = "A", ["B"] = "B", ["C"] = "C",
        ["D"] = "D", ["E"] = "E", ["F"] = "F",
        ["Enter"] = "GO",
        ["Backspace"] = "AD",
    }.ToFrozenDictionary();

    public static IReadOnlyDictionary<string, string> Map => KeyMap;

    public static string? MapToKim1Key(string keyName)
    {
        if (string.IsNullOrEmpty(keyName))
            return null;
        return KeyMap.TryGetValue(keyName, out var kimKey) ? kimKey : null;
    }
}
