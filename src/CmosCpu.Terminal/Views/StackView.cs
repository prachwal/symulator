using CmosCpu.Computer;
using Spectre.Console;

namespace CmosCpu.Terminal.Views;

public static class StackView
{
    public static void Render(ComputerMemoryBus memory, byte currentSp)
    {
        ushort stackBase = 0x0100;
        int stackSize = 0x100;
        ushort start = (ushort)(currentSp < 0xF0 ? stackBase : stackBase);

        byte[] data = memory.GetMemoryPage(start, stackSize);
        string dump = HexFormatter.FormatHexDump(data, start, 16);
        AnsiConsole.Write(new Panel(new Markup($"[grey]{dump.EscapeMarkup()}[/]"))
        {
            Header = new PanelHeader($"Stack (SP=0x{currentSp:X2})"),
            Border = BoxBorder.Rounded
        });
    }
}
