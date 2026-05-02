using Spectre.Console;

namespace CmosCpu.Terminal.Views;

public static class BreakpointsView
{
    public static void Render(IReadOnlySet<ushort> breakpoints)
    {
        if (breakpoints.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No breakpoints set[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("#");
        table.AddColumn("Address");

        int i = 1;
        foreach (var bp in breakpoints.OrderBy(x => x))
        {
            table.AddRow(i.ToString(), $"0x{bp:X4}");
            i++;
        }

        AnsiConsole.Write(table);
    }
}
