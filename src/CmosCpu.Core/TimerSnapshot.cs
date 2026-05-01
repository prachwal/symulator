namespace CmosCpu.Core;

public class TimerSnapshot
{
    public ushort Counter { get; init; }
    public byte Control { get; init; }
    public byte Status { get; init; }
    public bool Running { get; init; }
    public bool IrqEnabled { get; init; }
}
