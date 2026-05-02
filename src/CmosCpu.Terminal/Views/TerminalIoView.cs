using CmosCpu.Computer;
using Spectre.Console;

namespace CmosCpu.Terminal.Views;

public static class TerminalIoView
{
    public static void RenderTextDisplay(TextDisplayRegion? display)
    {
        if (display is null)
        {
            AnsiConsole.MarkupLine("[grey]No text display device[/]");
            return;
        }

        var panel = new Panel(BuildScreenContent(display))
        {
            Header = new PanelHeader($"Text Screen {display.Width}x{display.Height}"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0)
        };
        AnsiConsole.Write(panel);
    }

    public static void RenderApple1Terminal(Apple1PiaTerminalDevice? terminal)
    {
        if (terminal is null)
        {
            AnsiConsole.MarkupLine("[grey]No Apple-1 terminal device[/]");
            return;
        }

        string text = terminal.Text;
        if (string.IsNullOrEmpty(text))
            text = "(empty)";

        var panel = new Panel(new Markup($"[grey]{text.EscapeMarkup()}[/]"))
        {
            Header = new PanelHeader("Apple-1 Terminal"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0)
        };
        AnsiConsole.Write(panel);
    }

    public static string GetScreenText(TextDisplayRegion display)
    {
        var lines = new List<string>();
        for (int y = 0; y < display.Height; y++)
        {
            var chars = new char[display.Width];
            for (int x = 0; x < display.Width; x++)
                chars[x] = display.GetChar(x, y);
            lines.Add(new string(chars));
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildScreenContent(TextDisplayRegion display)
    {
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < display.Height; y++)
        {
            if (y > 0) sb.AppendLine();
            for (int x = 0; x < display.Width; x++)
                sb.Append(display.GetChar(x, y));
        }
        return sb.ToString();
    }
}
