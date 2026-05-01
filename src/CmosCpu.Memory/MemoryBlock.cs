using CmosCpu.Core;
using NLog;

namespace CmosCpu.Memory;

public abstract class MemoryBlock : IBusDevice
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    protected readonly byte[] Data;

    public ushort StartAddress { get; }
    public ushort EndAddress { get; }

    protected MemoryBlock(ushort startAddress, ushort endAddress)
    {
        StartAddress = startAddress;
        EndAddress = endAddress;
        int size = endAddress - startAddress + 1;
        Data = new byte[size];
    }

    public bool Contains(ushort address)
    {
        return address >= StartAddress && address <= EndAddress;
    }

    protected int ToOffset(ushort address)
    {
        return address - StartAddress;
    }

    public abstract byte Read(ushort address);
    public abstract void Write(ushort address, byte value);

    public void Load(byte[] program, ushort offset = 0)
    {
        int count = Math.Min(program.Length, Data.Length - offset);
        Array.Copy(program, 0, Data, offset, count);
        Logger.Debug("Loaded {Count} bytes at offset {Offset:X4} in {Start:X4}-{End:X4}", count, offset, StartAddress, EndAddress);
    }

    public byte[] Dump(ushort start, ushort end)
    {
        int startOff = ToOffset(start);
        int count = Math.Min(end - start + 1, Data.Length - startOff);
        byte[] result = new byte[count];
        Array.Copy(Data, startOff, result, 0, count);
        return result;
    }
}
