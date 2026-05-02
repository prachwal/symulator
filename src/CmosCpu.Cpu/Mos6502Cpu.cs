using CmosCpu.Core;

namespace CmosCpu.Cpu;

public sealed class Mos6502Cpu
{
    private readonly IMemoryBus _bus;
    private bool _irqLineActive;

    public byte A { get; private set; }
    public byte X { get; private set; }
    public byte Y { get; private set; }
    public ushort PC { get; private set; }
    public byte SP { get; private set; }

    public bool Carry { get; private set; }
    public bool Zero { get; private set; }
    public bool InterruptDisable { get; private set; }
    public bool Decimal { get; private set; }
    public bool Break { get; private set; }
    public bool Overflow { get; private set; }
    public bool Negative { get; private set; }

    /// <summary>
    /// CycleCount tracks documented instruction cycles, including branch/page-cross penalties.
    /// It does not model every individual bus read/write cycle.
    /// </summary>
    public ulong CycleCount { get; private set; }
    public bool IsHalted { get; set; }

    public Mos6502Cpu(IMemoryBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public void Reset()
    {
        A = 0;
        X = 0;
        Y = 0;
        SP = 0xFD;
        Carry = false;
        Zero = false;
        InterruptDisable = true;
        Decimal = false;
        Break = false;
        Overflow = false;
        Negative = false;
        CycleCount = 0;
        IsHalted = false;
        _irqLineActive = false;
        PC = ReadWord(0xFFFC);
        CycleCount += 7;
    }

    public int Step()
    {
        if (IsHalted)
            return 0;

        byte opcode = Read(PC);
        PC++;

        int cycles = Execute(opcode);
        CycleCount += (ulong)cycles;

        if (_irqLineActive && !InterruptDisable)
        {
            Irq();
        }

        return cycles;
    }

    public void SetIrqLine(bool active)
    {
        _irqLineActive = active;
    }

    public void Nmi()
    {
        PushWord(PC);
        PushStatus(false);
        InterruptDisable = true;
        PC = ReadWord(0xFFFA);
        CycleCount += 7;
    }

    public void Irq()
    {
        if (InterruptDisable)
            return;

        PushWord(PC);
        PushStatus(false);
        InterruptDisable = true;
        PC = ReadWord(0xFFFE);
        CycleCount += 7;
    }

    private byte Read(ushort addr)
    {
        return _bus.ReadByte(addr);
    }

    private void Write(ushort addr, byte value)
    {
        _bus.WriteByte(addr, value);
    }

    private ushort ReadWord(ushort addr)
    {
        byte lo = Read(addr);
        byte hi = Read((ushort)(addr + 1));
        return (ushort)((hi << 8) | lo);
    }

    private void Push(byte value)
    {
        Write((ushort)(0x0100 + SP), value);
        SP--;
    }

    private byte Pop()
    {
        SP++;
        return Read((ushort)(0x0100 + SP));
    }

    private void PushWord(ushort value)
    {
        Push((byte)((value >> 8) & 0xFF));
        Push((byte)(value & 0xFF));
    }

    private ushort PopWord()
    {
        byte lo = Pop();
        byte hi = Pop();
        return (ushort)((hi << 8) | lo);
    }

    private void PushStatus(bool breakFlag)
    {
        byte status = 0x20;
        if (Carry) status |= 0x01;
        if (Zero) status |= 0x02;
        if (InterruptDisable) status |= 0x04;
        if (Decimal) status |= 0x08;
        if (breakFlag) status |= 0x10;
        if (Overflow) status |= 0x40;
        if (Negative) status |= 0x80;
        Push(status);
    }

    private void SetZN(byte value)
    {
        Zero = value == 0;
        Negative = (value & 0x80) != 0;
    }

    private void PullStatus()
    {
        byte status = Pop();
        Carry = (status & 0x01) != 0;
        Zero = (status & 0x02) != 0;
        InterruptDisable = (status & 0x04) != 0;
        Decimal = (status & 0x08) != 0;
        Overflow = (status & 0x40) != 0;
        Negative = (status & 0x80) != 0;
    }

    private byte GetImmediate()
    {
        return Read(PC++);
    }

    private ushort GetZeroPage()
    {
        return Read(PC++);
    }

    private ushort GetZeroPageX()
    {
        return (byte)(Read(PC++) + X);
    }

    private ushort GetZeroPageY()
    {
        return (byte)(Read(PC++) + Y);
    }

    private ushort GetAbsolute()
    {
        byte lo = Read(PC++);
        byte hi = Read(PC++);
        return (ushort)((hi << 8) | lo);
    }

    private ushort GetAbsoluteXAddr()
    {
        ushort baseAddr = GetAbsolute();
        return (ushort)(baseAddr + X);
    }

    private ushort GetAbsoluteYAddr()
    {
        ushort baseAddr = GetAbsolute();
        return (ushort)(baseAddr + Y);
    }

    private ushort GetAbsoluteX(out bool pageCross)
    {
        ushort baseAddr = GetAbsolute();
        ushort result = (ushort)(baseAddr + X);
        pageCross = (baseAddr & 0xFF00) != (result & 0xFF00);
        return result;
    }

    private ushort GetAbsoluteY(out bool pageCross)
    {
        ushort baseAddr = GetAbsolute();
        ushort result = (ushort)(baseAddr + Y);
        pageCross = (baseAddr & 0xFF00) != (result & 0xFF00);
        return result;
    }

    private ushort GetIndirect()
    {
        ushort pointer = GetAbsolute();
        byte lo = Read(pointer);
        ushort hiAddr = (ushort)((pointer & 0xFF00) | ((pointer + 1) & 0x00FF));
        byte hi = Read(hiAddr);
        return (ushort)((hi << 8) | lo);
    }

    private ushort GetIndexedIndirect()
    {
        byte zp = (byte)(Read(PC++) + X);
        byte lo = Read(zp);
        byte hi = Read((byte)(zp + 1));
        return (ushort)((hi << 8) | lo);
    }

    private ushort GetIndirectIndexed(out bool pageCross)
    {
        byte zp = Read(PC++);
        byte lo = Read(zp);
        byte hi = Read((byte)(zp + 1));
        ushort baseAddr = (ushort)((hi << 8) | lo);
        ushort result = (ushort)(baseAddr + Y);
        pageCross = (baseAddr & 0xFF00) != (result & 0xFF00);
        return result;
    }

    private sbyte GetRelative()
    {
        return (sbyte)Read(PC++);
    }

    private int BranchIf(bool condition)
    {
        sbyte offset = GetRelative();

        if (!condition)
            return 2;

        ushort oldPc = PC;
        ushort newPc = (ushort)(PC + offset);
        PC = newPc;

        return (oldPc & 0xFF00) != (newPc & 0xFF00) ? 4 : 3;
    }

    private int Execute(byte opcode)
    {
        switch (opcode)
        {
            // --- ADC ---
            case 0x69: { byte v = GetImmediate(); AddWithCarry(v); return 2; }
            case 0x65: { byte v = Read(GetZeroPage()); AddWithCarry(v); return 3; }
            case 0x75: { byte v = Read(GetZeroPageX()); AddWithCarry(v); return 4; }
            case 0x6D: { byte v = Read(GetAbsolute()); AddWithCarry(v); return 4; }
            case 0x7D: { var addr = GetAbsoluteX(out var cx); byte v = Read(addr); AddWithCarry(v); return 4 + (cx ? 1 : 0); }
            case 0x79: { var addr = GetAbsoluteY(out var cy); byte v = Read(addr); AddWithCarry(v); return 4 + (cy ? 1 : 0); }
            case 0x61: { byte v = Read(GetIndexedIndirect()); AddWithCarry(v); return 6; }
            case 0x71: { var addr = GetIndirectIndexed(out var cy); byte v = Read(addr); AddWithCarry(v); return 5 + (cy ? 1 : 0); }

            // --- AND ---
            case 0x29: { A &= GetImmediate(); SetZN(A); return 2; }
            case 0x25: { A &= Read(GetZeroPage()); SetZN(A); return 3; }
            case 0x35: { A &= Read(GetZeroPageX()); SetZN(A); return 4; }
            case 0x2D: { A &= Read(GetAbsolute()); SetZN(A); return 4; }
            case 0x3D: { var addr = GetAbsoluteX(out var cx); A &= Read(addr); SetZN(A); return 4 + (cx ? 1 : 0); }
            case 0x39: { var addr = GetAbsoluteY(out var cy); A &= Read(addr); SetZN(A); return 4 + (cy ? 1 : 0); }
            case 0x21: { A &= Read(GetIndexedIndirect()); SetZN(A); return 6; }
            case 0x31: { var addr = GetIndirectIndexed(out var cy); A &= Read(addr); SetZN(A); return 5 + (cy ? 1 : 0); }

            // --- ASL ---
            case 0x0A: { A = ShiftLeft(A); return 2; }
            case 0x06: { var addr = GetZeroPage(); byte v = ShiftLeft(Read(addr)); Write(addr, v); return 5; }
            case 0x16: { var addr = GetZeroPageX(); byte v = ShiftLeft(Read(addr)); Write(addr, v); return 6; }
            case 0x0E: { var addr = GetAbsolute(); byte v = ShiftLeft(Read(addr)); Write(addr, v); return 6; }
            case 0x1E: { var addr = GetAbsoluteXAddr(); byte v = ShiftLeft(Read(addr)); Write(addr, v); return 7; }

            // --- BCC ---
            case 0x90: return BranchIf(!Carry);

            // --- BCS ---
            case 0xB0: return BranchIf(Carry);

            // --- BEQ ---
            case 0xF0: return BranchIf(Zero);

            // --- BIT ---
            case 0x24: { byte v = Read(GetZeroPage()); BitTest(v); return 3; }
            case 0x2C: { byte v = Read(GetAbsolute()); BitTest(v); return 4; }

            // --- BMI ---
            case 0x30: return BranchIf(Negative);

            // --- BNE ---
            case 0xD0: return BranchIf(!Zero);

            // --- BPL ---
            case 0x10: return BranchIf(!Negative);

            // --- BRK ---
            case 0x00: { PC++; PushWord(PC); PushStatus(true); InterruptDisable = true; PC = ReadWord(0xFFFE); return 7; }

            // --- BVC ---
            case 0x50: return BranchIf(!Overflow);

            // --- BVS ---
            case 0x70: return BranchIf(Overflow);

            // --- CLC ---
            case 0x18: { Carry = false; return 2; }

            // --- CLD ---
            case 0xD8: { Decimal = false; return 2; }

            // --- CLI ---
            case 0x58: { InterruptDisable = false; return 2; }

            // --- CLV ---
            case 0xB8: { Overflow = false; return 2; }

            // --- CMP ---
            case 0xC9: { byte v = GetImmediate(); Compare(A, v); return 2; }
            case 0xC5: { byte v = Read(GetZeroPage()); Compare(A, v); return 3; }
            case 0xD5: { byte v = Read(GetZeroPageX()); Compare(A, v); return 4; }
            case 0xCD: { byte v = Read(GetAbsolute()); Compare(A, v); return 4; }
            case 0xDD: { var addr = GetAbsoluteX(out var cx); byte v = Read(addr); Compare(A, v); return 4 + (cx ? 1 : 0); }
            case 0xD9: { var addr = GetAbsoluteY(out var cy); byte v = Read(addr); Compare(A, v); return 4 + (cy ? 1 : 0); }
            case 0xC1: { byte v = Read(GetIndexedIndirect()); Compare(A, v); return 6; }
            case 0xD1: { var addr = GetIndirectIndexed(out var cy); byte v = Read(addr); Compare(A, v); return 5 + (cy ? 1 : 0); }

            // --- CPX ---
            case 0xE0: { byte v = GetImmediate(); Compare(X, v); return 2; }
            case 0xE4: { byte v = Read(GetZeroPage()); Compare(X, v); return 3; }
            case 0xEC: { byte v = Read(GetAbsolute()); Compare(X, v); return 4; }

            // --- CPY ---
            case 0xC0: { byte v = GetImmediate(); Compare(Y, v); return 2; }
            case 0xC4: { byte v = Read(GetZeroPage()); Compare(Y, v); return 3; }
            case 0xCC: { byte v = Read(GetAbsolute()); Compare(Y, v); return 4; }

            // --- DEC ---
            case 0xC6: { var addr = GetZeroPage(); byte v = (byte)(Read(addr) - 1); Write(addr, v); SetZN(v); return 5; }
            case 0xD6: { var addr = GetZeroPageX(); byte v = (byte)(Read(addr) - 1); Write(addr, v); SetZN(v); return 6; }
            case 0xCE: { var addr = GetAbsolute(); byte v = (byte)(Read(addr) - 1); Write(addr, v); SetZN(v); return 6; }
            case 0xDE: { var addr = GetAbsoluteXAddr(); byte v = (byte)(Read(addr) - 1); Write(addr, v); SetZN(v); return 7; }

            // --- DEX ---
            case 0xCA: { X--; SetZN(X); return 2; }

            // --- DEY ---
            case 0x88: { Y--; SetZN(Y); return 2; }

            // --- EOR ---
            case 0x49: { A ^= GetImmediate(); SetZN(A); return 2; }
            case 0x45: { A ^= Read(GetZeroPage()); SetZN(A); return 3; }
            case 0x55: { A ^= Read(GetZeroPageX()); SetZN(A); return 4; }
            case 0x4D: { A ^= Read(GetAbsolute()); SetZN(A); return 4; }
            case 0x5D: { var addr = GetAbsoluteX(out var cx); A ^= Read(addr); SetZN(A); return 4 + (cx ? 1 : 0); }
            case 0x59: { var addr = GetAbsoluteY(out var cy); A ^= Read(addr); SetZN(A); return 4 + (cy ? 1 : 0); }
            case 0x41: { A ^= Read(GetIndexedIndirect()); SetZN(A); return 6; }
            case 0x51: { var addr = GetIndirectIndexed(out var cy); A ^= Read(addr); SetZN(A); return 5 + (cy ? 1 : 0); }

            // --- INC ---
            case 0xE6: { var addr = GetZeroPage(); byte v = (byte)(Read(addr) + 1); Write(addr, v); SetZN(v); return 5; }
            case 0xF6: { var addr = GetZeroPageX(); byte v = (byte)(Read(addr) + 1); Write(addr, v); SetZN(v); return 6; }
            case 0xEE: { var addr = GetAbsolute(); byte v = (byte)(Read(addr) + 1); Write(addr, v); SetZN(v); return 6; }
            case 0xFE: { var addr = GetAbsoluteXAddr(); byte v = (byte)(Read(addr) + 1); Write(addr, v); SetZN(v); return 7; }

            // --- INX ---
            case 0xE8: { X++; SetZN(X); return 2; }

            // --- INY ---
            case 0xC8: { Y++; SetZN(Y); return 2; }

            // --- JMP ---
            case 0x4C: { PC = GetAbsolute(); return 3; }
            case 0x6C: { PC = GetIndirect(); return 5; }

            // --- JSR ---
            case 0x20: { ushort addr = GetAbsolute(); PushWord((ushort)(PC - 1)); PC = addr; return 6; }

            // --- LDA ---
            case 0xA9: { A = GetImmediate(); SetZN(A); return 2; }
            case 0xA5: { A = Read(GetZeroPage()); SetZN(A); return 3; }
            case 0xB5: { A = Read(GetZeroPageX()); SetZN(A); return 4; }
            case 0xAD: { A = Read(GetAbsolute()); SetZN(A); return 4; }
            case 0xBD: { var addr = GetAbsoluteX(out var cx); A = Read(addr); SetZN(A); return 4 + (cx ? 1 : 0); }
            case 0xB9: { var addr = GetAbsoluteY(out var cy); A = Read(addr); SetZN(A); return 4 + (cy ? 1 : 0); }
            case 0xA1: { A = Read(GetIndexedIndirect()); SetZN(A); return 6; }
            case 0xB1: { var addr = GetIndirectIndexed(out var cy); A = Read(addr); SetZN(A); return 5 + (cy ? 1 : 0); }

            // --- LDX ---
            case 0xA2: { X = GetImmediate(); SetZN(X); return 2; }
            case 0xA6: { X = Read(GetZeroPage()); SetZN(X); return 3; }
            case 0xB6: { X = Read(GetZeroPageY()); SetZN(X); return 4; }
            case 0xAE: { X = Read(GetAbsolute()); SetZN(X); return 4; }
            case 0xBE: { var addr = GetAbsoluteY(out var cy); X = Read(addr); SetZN(X); return 4 + (cy ? 1 : 0); }

            // --- LDY ---
            case 0xA0: { Y = GetImmediate(); SetZN(Y); return 2; }
            case 0xA4: { Y = Read(GetZeroPage()); SetZN(Y); return 3; }
            case 0xB4: { Y = Read(GetZeroPageX()); SetZN(Y); return 4; }
            case 0xAC: { Y = Read(GetAbsolute()); SetZN(Y); return 4; }
            case 0xBC: { var addr = GetAbsoluteX(out var cx); Y = Read(addr); SetZN(Y); return 4 + (cx ? 1 : 0); }

            // --- LSR ---
            case 0x4A: { A = ShiftRight(A); return 2; }
            case 0x46: { var addr = GetZeroPage(); byte v = ShiftRight(Read(addr)); Write(addr, v); return 5; }
            case 0x56: { var addr = GetZeroPageX(); byte v = ShiftRight(Read(addr)); Write(addr, v); return 6; }
            case 0x4E: { var addr = GetAbsolute(); byte v = ShiftRight(Read(addr)); Write(addr, v); return 6; }
            case 0x5E: { var addr = GetAbsoluteXAddr(); byte v = ShiftRight(Read(addr)); Write(addr, v); return 7; }

            // --- NOP ---
            case 0xEA: { return 2; }

            // --- ORA ---
            case 0x09: { A |= GetImmediate(); SetZN(A); return 2; }
            case 0x05: { A |= Read(GetZeroPage()); SetZN(A); return 3; }
            case 0x15: { A |= Read(GetZeroPageX()); SetZN(A); return 4; }
            case 0x0D: { A |= Read(GetAbsolute()); SetZN(A); return 4; }
            case 0x1D: { var addr = GetAbsoluteX(out var cx); A |= Read(addr); SetZN(A); return 4 + (cx ? 1 : 0); }
            case 0x19: { var addr = GetAbsoluteY(out var cy); A |= Read(addr); SetZN(A); return 4 + (cy ? 1 : 0); }
            case 0x01: { A |= Read(GetIndexedIndirect()); SetZN(A); return 6; }
            case 0x11: { var addr = GetIndirectIndexed(out var cy); A |= Read(addr); SetZN(A); return 5 + (cy ? 1 : 0); }

            // --- PHA ---
            case 0x48: { Push(A); return 3; }

            // --- PHP ---
            case 0x08: { PushStatus(true); return 3; }

            // --- PLA ---
            case 0x68: { A = Pop(); SetZN(A); return 4; }

            // --- PLP ---
            case 0x28: { PullStatus(); return 4; }

            // --- ROL ---
            case 0x2A: { A = RotateLeft(A); return 2; }
            case 0x26: { var addr = GetZeroPage(); byte v = RotateLeft(Read(addr)); Write(addr, v); return 5; }
            case 0x36: { var addr = GetZeroPageX(); byte v = RotateLeft(Read(addr)); Write(addr, v); return 6; }
            case 0x2E: { var addr = GetAbsolute(); byte v = RotateLeft(Read(addr)); Write(addr, v); return 6; }
            case 0x3E: { var addr = GetAbsoluteXAddr(); byte v = RotateLeft(Read(addr)); Write(addr, v); return 7; }

            // --- ROR ---
            case 0x6A: { A = RotateRight(A); return 2; }
            case 0x66: { var addr = GetZeroPage(); byte v = RotateRight(Read(addr)); Write(addr, v); return 5; }
            case 0x76: { var addr = GetZeroPageX(); byte v = RotateRight(Read(addr)); Write(addr, v); return 6; }
            case 0x6E: { var addr = GetAbsolute(); byte v = RotateRight(Read(addr)); Write(addr, v); return 6; }
            case 0x7E: { var addr = GetAbsoluteXAddr(); byte v = RotateRight(Read(addr)); Write(addr, v); return 7; }

            // --- RTI ---
            case 0x40: { PullStatus(); PC = PopWord(); return 6; }

            // --- RTS ---
            case 0x60: { PC = (ushort)(PopWord() + 1); return 6; }

            // --- SBC ---
            case 0xE9: { byte v = GetImmediate(); SubtractWithCarry(v); return 2; }
            case 0xE5: { byte v = Read(GetZeroPage()); SubtractWithCarry(v); return 3; }
            case 0xF5: { byte v = Read(GetZeroPageX()); SubtractWithCarry(v); return 4; }
            case 0xED: { byte v = Read(GetAbsolute()); SubtractWithCarry(v); return 4; }
            case 0xFD: { var addr = GetAbsoluteX(out var cx); byte v = Read(addr); SubtractWithCarry(v); return 4 + (cx ? 1 : 0); }
            case 0xF9: { var addr = GetAbsoluteY(out var cy); byte v = Read(addr); SubtractWithCarry(v); return 4 + (cy ? 1 : 0); }
            case 0xE1: { byte v = Read(GetIndexedIndirect()); SubtractWithCarry(v); return 6; }
            case 0xF1: { var addr = GetIndirectIndexed(out var cy); byte v = Read(addr); SubtractWithCarry(v); return 5 + (cy ? 1 : 0); }

            // --- SEC ---
            case 0x38: { Carry = true; return 2; }

            // --- SED ---
            case 0xF8: { Decimal = true; return 2; }

            // --- SEI ---
            case 0x78: { InterruptDisable = true; return 2; }

            // --- STA ---
            case 0x85: { Write(GetZeroPage(), A); return 3; }
            case 0x95: { Write(GetZeroPageX(), A); return 4; }
            case 0x8D: { Write(GetAbsolute(), A); return 4; }
            case 0x9D: { Write(GetAbsoluteXAddr(), A); return 5; }
            case 0x99: { Write(GetAbsoluteYAddr(), A); return 5; }
            case 0x81: { Write(GetIndexedIndirect(), A); return 6; }
            case 0x91: { Write(GetIndirectIndexed(out _), A); return 6; }

            // --- STX ---
            case 0x86: { Write(GetZeroPage(), X); return 3; }
            case 0x96: { Write(GetZeroPageY(), X); return 4; }
            case 0x8E: { Write(GetAbsolute(), X); return 4; }

            // --- STY ---
            case 0x84: { Write(GetZeroPage(), Y); return 3; }
            case 0x94: { Write(GetZeroPageX(), Y); return 4; }
            case 0x8C: { Write(GetAbsolute(), Y); return 4; }

            // --- TAX ---
            case 0xAA: { X = A; SetZN(X); return 2; }

            // --- TAY ---
            case 0xA8: { Y = A; SetZN(Y); return 2; }

            // --- TSX ---
            case 0xBA: { X = SP; SetZN(X); return 2; }

            // --- TXA ---
            case 0x8A: { A = X; SetZN(A); return 2; }

            // --- TXS ---
            case 0x9A: { SP = X; return 2; }

            // --- TYA ---
            case 0x98: { A = Y; SetZN(A); return 2; }

            default:
                throw new InvalidOperationException($"Unknown 6502 opcode: 0x{opcode:X2}");
        }
    }

    private void AddWithCarry(byte value)
    {
        ushort sum;
        if (Decimal)
        {
            byte al = (byte)((A & 0x0F) + (value & 0x0F) + (Carry ? 1 : 0));
            if (al > 9) al = (byte)((al - 10) | 0x10);
            byte ah = (byte)(((A >> 4) & 0x0F) + ((value >> 4) & 0x0F) + (al > 15 ? 1 : 0));
            if (ah > 9) ah = (byte)((ah - 10) | 0x10);
            ushort bcdResult = (ushort)((ah << 4) | (al & 0x0F));
            Carry = bcdResult > 0x99;
            A = (byte)(bcdResult & 0xFF);
        }
        else
        {
            sum = (ushort)(A + value + (Carry ? 1 : 0));
            Carry = sum > 0xFF;
            Overflow = ((A ^ value) & 0x80) == 0 && ((A ^ (byte)sum) & 0x80) != 0;
            A = (byte)(sum & 0xFF);
        }
        SetZN(A);
    }

    private void SubtractWithCarry(byte value)
    {
        ushort diff;
        if (Decimal)
        {
            int al = (A & 0x0F) - (value & 0x0F) - (Carry ? 0 : 1);
            int lowBorrow = (al & 0x10) != 0 ? 1 : 0;
            if (lowBorrow != 0) al = (al - 6) & 0x0F;
            int ah = ((A >> 4) & 0x0F) - ((value >> 4) & 0x0F) - lowBorrow;
            int highBorrow = (ah & 0x10) != 0 ? 1 : 0;
            if (highBorrow != 0) ah = (ah - 6) & 0x0F;
            A = (byte)((ah << 4) | (al & 0x0F));
            Carry = highBorrow == 0;
        }
        else
        {
            diff = (ushort)(A - value - (Carry ? 0 : 1));
            Carry = diff <= 0xFF;
            Overflow = ((A ^ value) & 0x80) != 0 && ((A ^ (byte)diff) & 0x80) != 0;
            A = (byte)(diff & 0xFF);
        }
        SetZN(A);
    }

    private void Compare(byte reg, byte value)
    {
        Carry = reg >= value;
        SetZN((byte)(reg - value));
    }

    private void BitTest(byte value)
    {
        Zero = (A & value) == 0;
        Overflow = (value & 0x40) != 0;
        Negative = (value & 0x80) != 0;
    }

    private byte ShiftLeft(byte value)
    {
        Carry = (value & 0x80) != 0;
        byte result = (byte)(value << 1);
        SetZN(result);
        return result;
    }

    private byte ShiftRight(byte value)
    {
        Carry = (value & 0x01) != 0;
        byte result = (byte)(value >> 1);
        SetZN(result);
        return result;
    }

    private byte RotateLeft(byte value)
    {
        bool newCarry = (value & 0x80) != 0;
        byte result = (byte)((value << 1) | (Carry ? 1 : 0));
        Carry = newCarry;
        SetZN(result);
        return result;
    }

    private byte RotateRight(byte value)
    {
        bool newCarry = (value & 0x01) != 0;
        byte result = (byte)((value >> 1) | (Carry ? 0x80 : 0));
        Carry = newCarry;
        SetZN(result);
        return result;
    }
}
