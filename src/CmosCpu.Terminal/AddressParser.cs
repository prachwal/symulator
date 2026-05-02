namespace CmosCpu.Terminal;

public static class AddressParser
{
    public static ushort Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Address cannot be empty");

        string trimmed = value.Trim();

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ParseHex(trimmed[2..]);
        if (trimmed.StartsWith("$", StringComparison.OrdinalIgnoreCase))
            return ParseHex(trimmed[1..]);

        if (ushort.TryParse(trimmed, out _))
            return ushort.Parse(trimmed);

        return ParseHex(trimmed);
    }

    public static bool TryParse(string value, out ushort address)
    {
        address = 0;
        try
        {
            address = Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ushort ParseHex(string hex)
    {
        return Convert.ToUInt16(hex, 16);
    }
}
