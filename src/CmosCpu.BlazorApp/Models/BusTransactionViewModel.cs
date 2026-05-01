using CmosCpu.Core;

namespace CmosCpu.BlazorApp.Models;

public class BusTransactionViewModel
{
    public string Cycle { get; init; } = "";
    public string Operation { get; init; } = "";
    public string Address { get; init; } = "";
    public string Value { get; init; } = "";
    public string Device { get; init; } = "";

    public static BusTransactionViewModel FromTransaction(BusTransaction tx)
    {
        return new BusTransactionViewModel
        {
            Cycle = tx.Cycle.ToString(),
            Operation = tx.Operation == BusOperation.Read ? "READ" : "WRITE",
            Address = $"0x{tx.Address:X4}",
            Value = $"0x{tx.Value:X2}",
            Device = tx.DeviceName,
        };
    }
}
