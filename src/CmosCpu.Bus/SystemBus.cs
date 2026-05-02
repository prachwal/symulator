using CmosCpu.Core;

using NLog;

namespace CmosCpu.Bus;

public class SystemBus : IBus
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly List<IBusDevice> _devices = new();

    public event EventHandler<BusTransactionEventArgs>? Transaction;

    public void AttachDevice(IBusDevice device)
    {
        _devices.Add(device);
        Logger.Debug("Device attached: {Start:X4}-{End:X4}", device.StartAddress, device.EndAddress);
    }

    public void DetachDevice(IBusDevice device)
    {
        _devices.Remove(device);
    }

    public byte Read(ushort address)
    {
        foreach (var device in _devices)
        {
            if (device.Contains(address))
            {
                byte value = device.Read(address);
                Logger.Trace("BUS READ  {Address:X4} -> {Value:X2}", address, value);
                Transaction?.Invoke(this, new BusTransactionEventArgs
                {
                    Transaction = new BusTransaction
                    {
                        Cycle = 0,
                        Operation = BusOperation.Read,
                        Address = address,
                        Value = value,
                        DeviceName = device.GetType().Name
                    }
                });
                return value;
            }
        }
        Logger.Warn("BUS READ  {Address:X4} -> unmapped, returning 0", address);
        return 0;
    }

    public void Write(ushort address, byte value)
    {
        foreach (var device in _devices)
        {
            if (device.Contains(address))
            {
                device.Write(address, value);
                Logger.Trace("BUS WRITE {Address:X4} <- {Value:X2}", address, value);
                Transaction?.Invoke(this, new BusTransactionEventArgs
                {
                    Transaction = new BusTransaction
                    {
                        Cycle = 0,
                        Operation = BusOperation.Write,
                        Address = address,
                        Value = value,
                        DeviceName = device.GetType().Name
                    }
                });
                return;
            }
        }
        Logger.Warn("BUS WRITE {Address:X4} <- {Value:X2} -> unmapped, ignored", address, value);
    }

    public void ClearDevices()
    {
        _devices.Clear();
    }
}