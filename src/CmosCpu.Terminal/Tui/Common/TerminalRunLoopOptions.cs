namespace CmosCpu.Terminal.Tui.Common;

public sealed record TerminalRunLoopOptions(
    TimeSpan RefreshInterval,
    int InstructionsPerTick,
    int MaxCycles)
{
    public static TerminalRunLoopOptions Default { get; } = new(
        TimeSpan.FromMilliseconds(16),
        InstructionsPerTick: 100,
        MaxCycles: 10_000_000);
}
