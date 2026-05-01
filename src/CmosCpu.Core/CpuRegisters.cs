namespace CmosCpu.Core;

public class CpuRegisters
{
    public byte A { get; set; }
    public byte X { get; set; }
    public byte Y { get; set; }
    public ushort PC { get; set; }
    public byte SP { get; set; }
    public CpuFlags Flags { get; set; }
    public ulong CycleCount { get; set; }
    public bool Halted { get; set; }

    public bool ZeroFlag
    {
        get => Flags.HasFlag(CpuFlags.Zero);
        set => Flags = value ? Flags | CpuFlags.Zero : Flags & ~CpuFlags.Zero;
    }

    public bool CarryFlag
    {
        get => Flags.HasFlag(CpuFlags.Carry);
        set => Flags = value ? Flags | CpuFlags.Carry : Flags & ~CpuFlags.Carry;
    }

    public bool NegativeFlag
    {
        get => Flags.HasFlag(CpuFlags.Negative);
        set => Flags = value ? Flags | CpuFlags.Negative : Flags & ~CpuFlags.Negative;
    }

    public bool InterruptDisableFlag
    {
        get => Flags.HasFlag(CpuFlags.InterruptDisable);
        set => Flags = value ? Flags | CpuFlags.InterruptDisable : Flags & ~CpuFlags.InterruptDisable;
    }

    public void SetZeroAndNegativeFlags(byte value)
    {
        ZeroFlag = value == 0;
        NegativeFlag = (value & 0x80) != 0;
    }

    public CpuRegisters Clone()
    {
        return new CpuRegisters
        {
            A = A,
            X = X,
            Y = Y,
            PC = PC,
            SP = SP,
            Flags = Flags,
            CycleCount = CycleCount,
            Halted = Halted,
        };
    }
}
