namespace CmosCpu.Computer;

public sealed class KeyboardRegion
{
    private readonly Queue<byte> _keyBuffer = new();
    private byte _lastData;

    public ushort DataAddress { get; }
    public ushort StatusAddress { get; }

    public KeyboardRegion(ushort dataAddress, ushort statusAddress)
    {
        DataAddress = dataAddress;
        StatusAddress = statusAddress;
    }

    public void EnqueueKey(char c)
    {
        _keyBuffer.Enqueue((byte)c);
    }

    public byte ReadData()
    {
        if (_keyBuffer.Count > 0)
        {
            _lastData = _keyBuffer.Dequeue();
            return _lastData;
        }
        return _lastData;
    }

    public byte ReadStatus()
    {
        return (byte)(_keyBuffer.Count > 0 ? 1 : 0);
    }

    public int PendingCount => _keyBuffer.Count;
}
