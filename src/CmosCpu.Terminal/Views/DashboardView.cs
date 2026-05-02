using Spectre.Console;

namespace CmosCpu.Terminal.Views;

public static class DashboardView
{
    public static void Render(
        bool isLoaded,
        bool isRunning,
        long totalInstructions,
        ulong cycleCount,
        string? lastError,
        string cpuState,
        string registersLine,
        string profileName,
        int batchSize,
        int delayMs)
    {
        var table = new Table();
        table.Border = TableBorder.Rounded;
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Profile", profileName);
        table.AddRow("Status", isRunning ? "[green]Running[/]" : cpuState == "HALTED" ? "[red]HALTED[/]" : "[yellow]Stopped[/]");
        table.AddRow("Instructions", totalInstructions.ToString("N0"));
        table.AddRow("Cycles", cycleCount.ToString("N0"));
        table.AddRow("Batch", batchSize.ToString());
        table.AddRow("Delay", $"{delayMs} ms");
        table.AddRow("Registers", registersLine.EscapeMarkup());

        if (!string.IsNullOrEmpty(lastError))
            table.AddRow("Error", $"[red]{lastError.EscapeMarkup()}[/]");

        AnsiConsole.Write(table);
    }

    public static void RenderMinimalStatus(bool isRunning, string cpuState, long totalInstructions, ulong cycles, string? error)
    {
        string status = isRunning
            ? $"[green]RUNNING[/]  instructions={totalInstructions}  cycles={cycles}"
            : cpuState == "HALTED"
                ? $"[red]HALTED[/]  instructions={totalInstructions}  cycles={cycles}"
                : $"[yellow]STOPPED[/]  instructions={totalInstructions}  cycles={cycles}";

        if (!string.IsNullOrEmpty(error))
            status += $"  [red]ERROR: {error.EscapeMarkup()}[/]";

        AnsiConsole.MarkupLine(status);
    }
}
