namespace CmosCpu.Computer.Abstractions;

public interface IInterruptSource
{
    bool IrqPending { get; }
}
