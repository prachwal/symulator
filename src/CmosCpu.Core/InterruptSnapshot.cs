namespace CmosCpu.Core;

public class InterruptSnapshot
{
    public bool IrqPending { get; init; }
    public bool NmiPending { get; init; }
    public bool InterruptDisable { get; init; }
    public ushort ResetVector { get; init; }
    public ushort IrqVector { get; init; }
    public ushort NmiVector { get; init; }
}
