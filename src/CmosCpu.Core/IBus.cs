namespace CmosCpu.Core;

public interface IBus
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
    void AttachDevice(IBusDevice device);
    event EventHandler<BusTransactionEventArgs>? Transaction;
}
