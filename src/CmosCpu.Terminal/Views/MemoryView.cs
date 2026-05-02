using Spectre.Console;

namespace CmosCpu.Terminal.Views;

public static class MemoryView
{
    public static void Render(byte[] data, ushort startAddress, int bytesPerRow = 16)
    {
        string dump = HexFormatter.FormatHexDump(data, startAddress, bytesPerRow);
        AnsiConsole.Write(new Panel(new Markup($"[grey]{dump.EscapeMarkup()}[/]"))
        {
            Header = new PanelHeader($"Memory 0x{startAddress:X4}-0x{startAddress + data.Length:X4}"),
            Border = BoxBorder.Rounded
        });
    }

    public static string Format(byte[] data, ushort startAddress, int bytesPerRow = 16)
    {
        return HexFormatter.FormatHexDump(data, startAddress, bytesPerRow);
    }
}
