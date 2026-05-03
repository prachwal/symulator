namespace CmosCpu.Computer.Devices.Serial;

public sealed class UartSnapshot
{
    public required byte Status { get; init; }
    public required byte Control { get; init; }
    public required int RxCount { get; init; }
    public required int TxCount { get; init; }
    public required bool RxEnabled { get; init; }
    public required bool TxEnabled { get; init; }
}
