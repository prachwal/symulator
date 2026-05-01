namespace CmosCpu.Core;

public enum BusOperation
{
    Read,
    Write,
}

public class BusTransaction
{
    public ulong Cycle { get; init; }
    public BusOperation Operation { get; init; }
    public ushort Address { get; init; }
    public byte Value { get; init; }
    public string DeviceName { get; init; } = string.Empty;
}

public class BusTransactionEventArgs : EventArgs
{
    public BusTransaction Transaction { get; init; } = null!;
}
