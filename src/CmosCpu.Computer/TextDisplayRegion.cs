namespace CmosCpu.Computer;

public sealed class TextDisplayRegion
{
    private readonly byte[] _buffer;
    private long _version;

    public ushort StartAddress { get; }
    public int Width { get; }
    public int Height { get; }
    public long Version => _version;

    public TextDisplayRegion(ushort startAddress, int width, int height)
    {
        StartAddress = startAddress;
        Width = width;
        Height = height;
        _buffer = new byte[width * height];
    }

    public byte Read(ushort address)
    {
        int offset = address - StartAddress;
        if (offset < 0 || offset >= _buffer.Length)
            return 0;
        return _buffer[offset];
    }

    public void Write(ushort address, byte value)
    {
        int offset = address - StartAddress;
        if (offset < 0 || offset >= _buffer.Length)
            return;
        _buffer[offset] = value;
        _version++;
    }

    public byte[] GetBuffer() => _buffer;

    public char GetChar(int x, int y)
    {
        int idx = y * Width + x;
        if (idx < 0 || idx >= _buffer.Length)
            return ' ';
        byte b = _buffer[idx];
        return b >= 32 && b < 127 ? (char)b : '.';
    }

    public void Clear()
    {
        Array.Clear(_buffer);
        _version++;
    }
}
