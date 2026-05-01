namespace CmosCpu.Core;

public class TraceEventArgs : EventArgs
{
    public TraceEntry Entry { get; init; } = null!;
}
