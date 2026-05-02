namespace CmosCpu.BlazorApp.Services;

public static class AddressParser
{
    public static bool TryParseAddress(string text, out ushort address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..];
        else if (trimmed.StartsWith("$", StringComparison.Ordinal))
            trimmed = trimmed[1..];

        if (ushort.TryParse(trimmed,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed))
        {
            address = parsed;
            return true;
        }

        return false;
    }
}