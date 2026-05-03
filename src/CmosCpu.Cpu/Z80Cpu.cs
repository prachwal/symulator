using CmosCpu.Core;

namespace CmosCpu.Cpu;

public interface IZ80IoBus
{
    byte ReadPort(ushort port);
    void WritePort(ushort port, byte value);
}

public sealed record Z80StepResult(ushort PcBefore, byte Opcode, int Cycles, string Mnemonic, bool Halted);

public sealed class Z80Cpu
{
    private readonly IMemoryBus _memory;
    private readonly IZ80IoBus _io;

    public Z80Registers Registers { get; } = new();
    public bool IsHalted { get; private set; }
    public bool Iff1 { get; private set; }
    public bool Iff2 { get; private set; }
    public int InterruptMode { get; private set; }
    public ulong CycleCount { get; private set; }

    public Z80Cpu(IMemoryBus memory, IZ80IoBus io)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
        _io = io ?? throw new ArgumentNullException(nameof(io));
    }

    public void SetProgramCounter(ushort address) => Registers.PC = address;

    public void Reset()
    {
        Registers.Reset();
        IsHalted = false;
        Iff1 = false;
        Iff2 = false;
        InterruptMode = 0;
        CycleCount = 0;
    }

    public int Step() => StepDetailed().Cycles;

    public Z80StepResult StepDetailed()
    {
        if (IsHalted)
        {
            Registers.IncrementRefreshRegister();
            CycleCount += 4;
            return new Z80StepResult(Registers.PC, 0x76, 4, "HALT", true);
        }

        ushort pcBefore = Registers.PC;
        byte opcode = FetchByte();
        Z80StepResult result = Execute(opcode, pcBefore);
        CycleCount += (ulong)result.Cycles;
        return result;
    }

    private Z80StepResult Execute(byte opcode, ushort pcBefore) => opcode switch
    {
        0x00 => Result(pcBefore, opcode, 4, "NOP"),
        0x76 => Halt(pcBefore, opcode),

        0x3E => Ld8(pcBefore, opcode, Z80Register8.A, "LD A,n"),
        0x06 => Ld8(pcBefore, opcode, Z80Register8.B, "LD B,n"),
        0x0E => Ld8(pcBefore, opcode, Z80Register8.C, "LD C,n"),
        0x16 => Ld8(pcBefore, opcode, Z80Register8.D, "LD D,n"),
        0x1E => Ld8(pcBefore, opcode, Z80Register8.E, "LD E,n"),
        0x26 => Ld8(pcBefore, opcode, Z80Register8.H, "LD H,n"),
        0x2E => Ld8(pcBefore, opcode, Z80Register8.L, "LD L,n"),

        0x01 => Ld16(pcBefore, opcode, Z80Register16.BC, "LD BC,nn"),
        0x11 => Ld16(pcBefore, opcode, Z80Register16.DE, "LD DE,nn"),
        0x21 => Ld16(pcBefore, opcode, Z80Register16.HL, "LD HL,nn"),
        0x31 => Ld16(pcBefore, opcode, Z80Register16.SP, "LD SP,nn"),

        0x3A => LdAFromAddress(pcBefore, opcode),
        0x32 => LdAddressFromA(pcBefore, opcode),
        0xD3 => OutImmediatePortA(pcBefore, opcode),
        0xDB => InAImmediatePort(pcBefore, opcode),
        0xC3 => Jp(pcBefore, opcode),

        _ => throw new InvalidOperationException($"Unknown or unsupported Z80 opcode: 0x{opcode:X2} at PC=0x{pcBefore:X4}")
    };

    private Z80StepResult Result(ushort pcBefore, byte opcode, int cycles, string mnemonic) =>
        new(pcBefore, opcode, cycles, mnemonic, IsHalted);

    private Z80StepResult Halt(ushort pcBefore, byte opcode)
    {
        IsHalted = true;
        return Result(pcBefore, opcode, 4, "HALT");
    }

    private Z80StepResult Ld8(ushort pcBefore, byte opcode, Z80Register8 target, string mnemonic)
    {
        Registers.Set8(target, FetchByte());
        return Result(pcBefore, opcode, 7, mnemonic);
    }

    private Z80StepResult Ld16(ushort pcBefore, byte opcode, Z80Register16 target, string mnemonic)
    {
        Registers.Set16(target, FetchWord());
        return Result(pcBefore, opcode, 10, mnemonic);
    }

    private Z80StepResult LdAFromAddress(ushort pcBefore, byte opcode)
    {
        Registers.A = _memory.ReadByte(FetchWord());
        return Result(pcBefore, opcode, 13, "LD A,(nn)");
    }

    private Z80StepResult LdAddressFromA(ushort pcBefore, byte opcode)
    {
        _memory.WriteByte(FetchWord(), Registers.A);
        return Result(pcBefore, opcode, 13, "LD (nn),A");
    }

    private Z80StepResult OutImmediatePortA(ushort pcBefore, byte opcode)
    {
        byte portLow = FetchByte();
        ushort port = (ushort)((Registers.A << 8) | portLow);
        _io.WritePort(port, Registers.A);
        return Result(pcBefore, opcode, 11, "OUT (n),A");
    }

    private Z80StepResult InAImmediatePort(ushort pcBefore, byte opcode)
    {
        byte portLow = FetchByte();
        ushort port = (ushort)((Registers.A << 8) | portLow);
        Registers.A = _io.ReadPort(port);
        return Result(pcBefore, opcode, 11, "IN A,(n)");
    }

    private Z80StepResult Jp(ushort pcBefore, byte opcode)
    {
        Registers.PC = FetchWord();
        return Result(pcBefore, opcode, 10, "JP nn");
    }

    private byte FetchByte()
    {
        byte value = _memory.ReadByte(Registers.PC);
        Registers.PC++;
        Registers.IncrementRefreshRegister();
        return value;
    }

    private ushort FetchWord()
    {
        byte low = FetchByte();
        byte high = FetchByte();
        return (ushort)(low | (high << 8));
    }
}

public sealed class Z80Registers
{
    public byte A { get; set; }
    public byte F { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }
    public byte AlternateA { get; set; }
    public byte AlternateF { get; set; }
    public byte AlternateB { get; set; }
    public byte AlternateC { get; set; }
    public byte AlternateD { get; set; }
    public byte AlternateE { get; set; }
    public byte AlternateH { get; set; }
    public byte AlternateL { get; set; }
    public ushort IX { get; set; }
    public ushort IY { get; set; }
    public ushort SP { get; set; }
    public ushort PC { get; set; }
    public byte I { get; set; }
    public byte R { get; private set; }

    public ushort AF { get => Combine(A, F); set { A = High(value); F = Low(value); } }
    public ushort BC { get => Combine(B, C); set { B = High(value); C = Low(value); } }
    public ushort DE { get => Combine(D, E); set { D = High(value); E = Low(value); } }
    public ushort HL { get => Combine(H, L); set { H = High(value); L = Low(value); } }

    public bool SignFlag { get => GetFlag(Z80Flag.Sign); set => SetFlag(Z80Flag.Sign, value); }
    public bool ZeroFlag { get => GetFlag(Z80Flag.Zero); set => SetFlag(Z80Flag.Zero, value); }
    public bool HalfCarryFlag { get => GetFlag(Z80Flag.HalfCarry); set => SetFlag(Z80Flag.HalfCarry, value); }
    public bool ParityOverflowFlag { get => GetFlag(Z80Flag.ParityOverflow); set => SetFlag(Z80Flag.ParityOverflow, value); }
    public bool AddSubtractFlag { get => GetFlag(Z80Flag.AddSubtract); set => SetFlag(Z80Flag.AddSubtract, value); }
    public bool CarryFlag { get => GetFlag(Z80Flag.Carry); set => SetFlag(Z80Flag.Carry, value); }

    public void Reset()
    {
        A = F = B = C = D = E = H = L = 0;
        AlternateA = AlternateF = AlternateB = AlternateC = AlternateD = AlternateE = AlternateH = AlternateL = 0;
        IX = IY = SP = PC = 0;
        I = R = 0;
    }

    public byte Get8(Z80Register8 register) => register switch
    {
        Z80Register8.A => A,
        Z80Register8.F => F,
        Z80Register8.B => B,
        Z80Register8.C => C,
        Z80Register8.D => D,
        Z80Register8.E => E,
        Z80Register8.H => H,
        Z80Register8.L => L,
        _ => throw new ArgumentOutOfRangeException(nameof(register), register, null)
    };

    public void Set8(Z80Register8 register, byte value)
    {
        switch (register)
        {
            case Z80Register8.A: A = value; break;
            case Z80Register8.F: F = value; break;
            case Z80Register8.B: B = value; break;
            case Z80Register8.C: C = value; break;
            case Z80Register8.D: D = value; break;
            case Z80Register8.E: E = value; break;
            case Z80Register8.H: H = value; break;
            case Z80Register8.L: L = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(register), register, null);
        }
    }

    public ushort Get16(Z80Register16 register) => register switch
    {
        Z80Register16.AF => AF,
        Z80Register16.BC => BC,
        Z80Register16.DE => DE,
        Z80Register16.HL => HL,
        Z80Register16.IX => IX,
        Z80Register16.IY => IY,
        Z80Register16.SP => SP,
        Z80Register16.PC => PC,
        _ => throw new ArgumentOutOfRangeException(nameof(register), register, null)
    };

    public void Set16(Z80Register16 register, ushort value)
    {
        switch (register)
        {
            case Z80Register16.AF: AF = value; break;
            case Z80Register16.BC: BC = value; break;
            case Z80Register16.DE: DE = value; break;
            case Z80Register16.HL: HL = value; break;
            case Z80Register16.IX: IX = value; break;
            case Z80Register16.IY: IY = value; break;
            case Z80Register16.SP: SP = value; break;
            case Z80Register16.PC: PC = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(register), register, null);
        }
    }

    public void ExchangeAF()
    {
        (A, AlternateA) = (AlternateA, A);
        (F, AlternateF) = (AlternateF, F);
    }

    public void ExchangeGeneralRegisters()
    {
        (B, AlternateB) = (AlternateB, B);
        (C, AlternateC) = (AlternateC, C);
        (D, AlternateD) = (AlternateD, D);
        (E, AlternateE) = (AlternateE, E);
        (H, AlternateH) = (AlternateH, H);
        (L, AlternateL) = (AlternateL, L);
    }

    public void IncrementRefreshRegister() => R++;

    private bool GetFlag(Z80Flag flag) => (F & (byte)flag) != 0;

    private void SetFlag(Z80Flag flag, bool value)
    {
        if (value)
            F |= (byte)flag;
        else
            F &= (byte)~(byte)flag;
    }

    private static ushort Combine(byte high, byte low) => (ushort)((high << 8) | low);
    private static byte High(ushort value) => (byte)(value >> 8);
    private static byte Low(ushort value) => (byte)(value & 0xFF);
}

public enum Z80Register8 { A, F, B, C, D, E, H, L }
public enum Z80Register16 { AF, BC, DE, HL, IX, IY, SP, PC }

[Flags]
public enum Z80Flag : byte
{
    Carry = 0b0000_0001,
    AddSubtract = 0b0000_0010,
    ParityOverflow = 0b0000_0100,
    UndocumentedX = 0b0000_1000,
    HalfCarry = 0b0001_0000,
    UndocumentedY = 0b0010_0000,
    Zero = 0b0100_0000,
    Sign = 0b1000_0000
}
