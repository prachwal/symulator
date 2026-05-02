using CmosCpu.Core;
using FluentAssertions;

namespace CmosCpu.Cpu.Tests;

[TestClass]
public sealed class Mos6502ResetTests
{
    private static Mos6502Cpu CreateCpu(out Ram64K ram)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, 0x00);
        ram.WriteByte(0xFFFD, 0x80);
        return new Mos6502Cpu(ram);
    }

    [TestMethod]
    public void Reset_LoadsPCFromVector()
    {
        var cpu = CreateCpu(out _);
        cpu.Reset();
        cpu.PC.Should().Be(0x8000);
    }

    [TestMethod]
    public void Reset_SetsSPtoFD()
    {
        var cpu = CreateCpu(out _);
        cpu.Reset();
        cpu.SP.Should().Be(0xFD);
    }

    [TestMethod]
    public void Reset_SetsInterruptDisable()
    {
        var cpu = CreateCpu(out _);
        cpu.Reset();
        cpu.InterruptDisable.Should().BeTrue();
    }

    [TestMethod]
    public void Reset_ClearsDecimal()
    {
        var cpu = CreateCpu(out _);
        cpu.Reset();
        cpu.Decimal.Should().BeFalse();
    }
}

[TestClass]
public sealed class Mos6502LoadStoreTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void LDA_Immediate()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x42 }, out _);
        cpu.Step();
        cpu.A.Should().Be(0x42);
        cpu.Zero.Should().BeFalse();
        cpu.Negative.Should().BeFalse();
    }

    [TestMethod]
    public void LDA_Immediate_ZeroSetsFlag()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x00 }, out _);
        cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void LDA_Immediate_NegativeSetsFlag()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0xFF }, out _);
        cpu.Step();
        cpu.Negative.Should().BeTrue();
    }

    [TestMethod]
    public void LDX_Immediate()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x55 }, out _);
        cpu.Step();
        cpu.X.Should().Be(0x55);
    }

    [TestMethod]
    public void LDY_Immediate()
    {
        var cpu = CreateCpu(new byte[] { 0xA0, 0x77 }, out _);
        cpu.Step();
        cpu.Y.Should().Be(0x77);
    }

    [TestMethod]
    public void STA_Absolute()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0xAB, 0x8D, 0x00, 0x90 }, out var ram);
        cpu.Step();
        cpu.Step();
        ram.ReadByte(0x9000).Should().Be(0xAB);
    }

    [TestMethod]
    public void STX_ZeroPage()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0xCD, 0x86, 0x20 }, out var ram);
        cpu.Step();
        cpu.Step();
        ram.ReadByte(0x0020).Should().Be(0xCD);
    }

    [TestMethod]
    public void STY_ZeroPage()
    {
        var cpu = CreateCpu(new byte[] { 0xA0, 0xEF, 0x84, 0x30 }, out var ram);
        cpu.Step();
        cpu.Step();
        ram.ReadByte(0x0030).Should().Be(0xEF);
    }
}

[TestClass]
public sealed class Mos6502ArithmeticTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void ADC_Simple()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x10, 0x69, 0x20 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x30);
        cpu.Carry.Should().BeFalse();
    }

    [TestMethod]
    public void ADC_CarrySet()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0xFF, 0x69, 0x01 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void ADC_Overflow()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x7F, 0x69, 0x01 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x80);
        cpu.Overflow.Should().BeTrue();
        cpu.Negative.Should().BeTrue();
    }

    [TestMethod]
    public void SBC_Simple()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x30, 0x38, 0xE9, 0x10 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x20);
        cpu.Carry.Should().BeTrue();
    }

    [TestMethod]
    public void CMP_Equal()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x50, 0xC9, 0x50 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void CPX_Greater()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x60, 0xE0, 0x40 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeFalse();
    }

    [TestMethod]
    public void CPY_Less()
    {
        var cpu = CreateCpu(new byte[] { 0xA0, 0x20, 0xC0, 0x40 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Carry.Should().BeFalse();
        cpu.Negative.Should().BeTrue();
    }

    [TestMethod]
    public void ADC_Decimal_15Plus27()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x15, 0x69, 0x27 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x42);
        cpu.Carry.Should().BeFalse();
        cpu.Zero.Should().BeFalse();
    }

    [TestMethod]
    public void ADC_Decimal_50Plus50_Carry()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x50, 0x69, 0x50 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void ADC_Decimal_99Plus01_Carry()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x99, 0x69, 0x01 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void SBC_Decimal_42Minus27()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x42, 0x38, 0xE9, 0x27 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x15);
        cpu.Carry.Should().BeTrue();
    }

    [TestMethod]
    public void SBC_Decimal_0Minus1_Borrow()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x00, 0x38, 0xE9, 0x01 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x99);
        cpu.Carry.Should().BeFalse();
        cpu.Zero.Should().BeFalse();
    }

    [TestMethod]
    public void ADC_Decimal_Flags_ZeroAndNegative()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x00, 0x69, 0x00 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Zero.Should().BeTrue();
        cpu.Negative.Should().BeFalse();
    }

    [TestMethod]
    public void ADC_Decimal_NegativeFlag()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x80, 0x69, 0x00 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x80);
        cpu.Negative.Should().BeTrue();
    }
}

[TestClass]
public sealed class Mos6502BranchTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void BEQ_TakenWhenZero()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x00, 0xF0, 0x05 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.PC.Should().Be(0x8009);
    }

    [TestMethod]
    public void BNE_NotTakenWhenZero()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x00, 0xD0, 0x05 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.PC.Should().Be(0x8004);
    }

    [TestMethod]
    public void Branch_PositiveOffset()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x01, 0xD0, 0x10 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.PC.Should().Be(0x8014);
    }

    [TestMethod]
    public void Branch_NegativeOffset()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x01, 0xD0, 0xFC }, out _);
        cpu.Step();
        cpu.Step();
        cpu.PC.Should().Be(0x8000);
    }

    [TestMethod]
    public void BCS_TakenWhenCarry()
    {
        var cpu = CreateCpu(new byte[] { 0x38, 0xB0, 0x03 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.PC.Should().Be(0x8006);
    }

    [TestMethod]
    public void BPL_TakenWhenPositive()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x01, 0x10, 0x03 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.PC.Should().Be(0x8007);
    }

    [TestMethod]
    public void Branch_NotTaken_Returns2Cycles()
    {
        var cpu = CreateCpu(new byte[] { 0x18, 0xB0, 0x05 }, out _);
        cpu.Step();
        var cycles = cpu.Step();
        cycles.Should().Be(2);
    }

    [TestMethod]
    public void Branch_TakenSamePage_Returns3Cycles()
    {
        var cpu = CreateCpu(new byte[] { 0x38, 0xB0, 0x05 }, out _);
        cpu.Step();
        var cycles = cpu.Step();
        cycles.Should().Be(3);
    }

    [TestMethod]
    public void Branch_TakenPageCross_Returns4Cycles()
    {
        var cpu = CreateCpu(new byte[] { 0x38, 0xB0, 0x10 }, out _, origin: 0x80F0);
        cpu.Step();
        var cycles = cpu.Step();
        cycles.Should().Be(4);
        cpu.PC.Should().Be(0x8103);
    }
}

[TestClass]
public sealed class Mos6502StackTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void PHA_PLA()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x42, 0x48, 0xA9, 0x00, 0x68 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Step();
        cpu.A.Should().Be(0x42);
    }

    [TestMethod]
    public void PHP_PLP()
    {
        var cpu = CreateCpu(new byte[] { 0x38, 0x08, 0x18, 0x28 }, out _);
        cpu.Step();
        cpu.Carry.Should().BeTrue();
        cpu.Step();
        cpu.Step();
        cpu.Carry.Should().BeFalse();
        cpu.Step();
        cpu.Carry.Should().BeTrue();
    }

    [TestMethod]
    public void JSR_RTS()
    {
        var cpu = CreateCpu(new byte[] { 0x20, 0x05, 0x80, 0xEA, 0xEA, 0x60 }, out _);
        cpu.Step();
        cpu.PC.Should().Be(0x8005);
        cpu.Step();
        cpu.PC.Should().Be(0x8003);
    }
}

[TestClass]
public sealed class Mos6502JumpTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void JMP_Absolute()
    {
        var cpu = CreateCpu(new byte[] { 0x4C, 0x00, 0x90 }, out _);
        cpu.Step();
        cpu.PC.Should().Be(0x9000);
    }

    [TestMethod]
    public void JMP_Indirect()
    {
        var ram = new Ram64K();
        ram.WriteByte(0xFFFC, 0x00);
        ram.WriteByte(0xFFFD, 0x80);
        ram.WriteByte(0x8000, 0x6C);
        ram.WriteByte(0x8001, 0x00);
        ram.WriteByte(0x8002, 0x90);
        ram.WriteByte(0x9000, 0x34);
        ram.WriteByte(0x9001, 0x12);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        cpu.Step();
        cpu.PC.Should().Be(0x1234);
    }

    [TestMethod]
    public void JMP_Indirect_PageWrapBug()
    {
        var ram = new Ram64K();
        ram.WriteByte(0xFFFC, 0x00);
        ram.WriteByte(0xFFFD, 0x80);
        ram.WriteByte(0x8000, 0x6C);
        ram.WriteByte(0x8001, 0xFF);
        ram.WriteByte(0x8002, 0x90);
        ram.WriteByte(0x90FF, 0x34);
        ram.WriteByte(0x9000, 0x12);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        cpu.Step();
        cpu.PC.Should().Be(0x1234);
    }
}

[TestClass]
public sealed class Mos6502InterruptTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void IRQ_IgnoredWhenInterruptDisable()
    {
        var cpu = CreateCpu(new byte[] { 0xEA, 0xEA }, out var ram);
        ram.WriteByte(0xFFFE, 0x00);
        ram.WriteByte(0xFFFF, 0x90);
        cpu.Irq();
        cpu.PC.Should().Be(0x8000);
    }

    [TestMethod]
    public void IRQ_HandledWhenEnabled()
    {
        var cpu = CreateCpu(new byte[] { 0x58, 0xEA }, out var ram);
        ram.WriteByte(0xFFFE, 0x00);
        ram.WriteByte(0xFFFF, 0x90);
        cpu.Step();
        cpu.Irq();
        cpu.PC.Should().Be(0x9000);
        cpu.InterruptDisable.Should().BeTrue();
    }

    [TestMethod]
    public void NMI_AlwaysHandled()
    {
        var cpu = CreateCpu(new byte[] { 0xEA }, out var ram);
        ram.WriteByte(0xFFFA, 0x00);
        ram.WriteByte(0xFFFB, 0x90);
        cpu.Nmi();
        cpu.PC.Should().Be(0x9000);
        cpu.InterruptDisable.Should().BeTrue();
    }

    [TestMethod]
    public void RTI_RestoresPCAndStatus()
    {
        var cpu = CreateCpu(new byte[] { 0x58, 0x40 }, out var ram);
        ram.WriteByte(0xFFFE, 0x00);
        ram.WriteByte(0xFFFF, 0x90);
        ram.WriteByte(0x9000, 0x40);
        cpu.Step();
        cpu.Irq();
        cpu.Step();
        cpu.PC.Should().Be(0x8001);
    }
}

[TestClass]
public sealed class Mos6502ShiftTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void ASL_Accumulator()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x01, 0x0A }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x02);
        cpu.Carry.Should().BeFalse();
    }

    [TestMethod]
    public void ASL_Accumulator_CarrySet()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x80, 0x0A }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
    }

    [TestMethod]
    public void LSR_Accumulator()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x04, 0x4A }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x02);
        cpu.Carry.Should().BeFalse();
    }

    [TestMethod]
    public void LSR_Accumulator_CarrySet()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x01, 0x4A }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
    }

    [TestMethod]
    public void ROL_Accumulator()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x40, 0x2A }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x80);
        cpu.Carry.Should().BeFalse();
    }

    [TestMethod]
    public void ROR_Accumulator()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x02, 0x6A }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x01);
        cpu.Carry.Should().BeFalse();
    }

    [TestMethod]
    public void ROR_Accumulator_RotateThroughCarry()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x01, 0x6A, 0x6A }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Carry.Should().BeTrue();

        cpu.Step();
        cpu.A.Should().Be(0x80);
        cpu.Carry.Should().BeFalse();
    }
}

[TestClass]
public sealed class Mos6502FlagTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void CLC_ClearsCarry()
    {
        var cpu = CreateCpu(new byte[] { 0x38, 0x18 }, out _);
        cpu.Step();
        cpu.Carry.Should().BeTrue();
        cpu.Step();
        cpu.Carry.Should().BeFalse();
    }

    [TestMethod]
    public void SEI_SetsInterruptDisable()
    {
        var cpu = CreateCpu(new byte[] { 0x58, 0x78 }, out _);
        cpu.Step();
        cpu.InterruptDisable.Should().BeFalse();
        cpu.Step();
        cpu.InterruptDisable.Should().BeTrue();
    }

    [TestMethod]
    public void RegisterTransfers_SetFlags()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x00, 0xAA }, out _);
        cpu.Step();
        cpu.Step();
        cpu.X.Should().Be(0x00);
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void BIT_SetsFlags()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x00, 0x2C, 0x00, 0x90 }, out var ram);
        ram.WriteByte(0x9000, 0xC0);
        cpu.Step();
        cpu.Step();
        cpu.Negative.Should().BeTrue();
        cpu.Overflow.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void UnknownOpcode_Throws()
    {
        var cpu = CreateCpu(new byte[] { 0x02 }, out _);
        Action act = () => cpu.Step();
        act.Should().Throw<InvalidOperationException>();
    }
}

[TestClass]
public sealed class Mos6502CompletenessTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    private static readonly byte[] OfficialOpcodes =
    [
        0x00, 0x69, 0x65, 0x75, 0x6D, 0x7D, 0x79, 0x61, 0x71, // ADC
        0x29, 0x25, 0x35, 0x2D, 0x3D, 0x39, 0x21, 0x31, // AND
        0x0A, 0x06, 0x16, 0x0E, 0x1E, // ASL
        0x90, 0xB0, 0xF0, 0xD0, 0x30, 0x10, 0x50, 0x70, // branches
        0x24, 0x2C, // BIT
        0x00, // BRK (already listed)
        0x18, 0xD8, 0x58, 0xB8, // CLC, CLD, CLI, CLV
        0xC9, 0xC5, 0xD5, 0xCD, 0xDD, 0xD9, 0xC1, 0xD1, // CMP
        0xE0, 0xE4, 0xEC, // CPX
        0xC0, 0xC4, 0xCC, // CPY
        0xC6, 0xD6, 0xCE, 0xDE, // DEC
        0xCA, 0x88, // DEX, DEY
        0x49, 0x45, 0x55, 0x4D, 0x5D, 0x59, 0x41, 0x51, // EOR
        0xE6, 0xF6, 0xEE, 0xFE, // INC
        0xE8, 0xC8, // INX, INY
        0x4C, 0x6C, // JMP
        0x20, // JSR
        0xA9, 0xA5, 0xB5, 0xAD, 0xBD, 0xB9, 0xA1, 0xB1, // LDA
        0xA2, 0xA6, 0xB6, 0xAE, 0xBE, // LDX
        0xA0, 0xA4, 0xB4, 0xAC, 0xBC, // LDY
        0x4A, 0x46, 0x56, 0x4E, 0x5E, // LSR
        0xEA, // NOP
        0x09, 0x05, 0x15, 0x0D, 0x1D, 0x19, 0x01, 0x11, // ORA
        0x48, 0x08, 0x68, 0x28, // PHA, PHP, PLA, PLP
        0x2A, 0x26, 0x36, 0x2E, 0x3E, // ROL
        0x6A, 0x66, 0x76, 0x6E, 0x7E, // ROR
        0x40, 0x60, // RTI, RTS
        0xE9, 0xE5, 0xF5, 0xED, 0xFD, 0xF9, 0xE1, 0xF1, // SBC
        0x38, 0xF8, 0x78, // SEC, SED, SEI
        0x85, 0x95, 0x8D, 0x9D, 0x99, 0x81, 0x91, // STA
        0x86, 0x96, 0x8E, // STX
        0x84, 0x94, 0x8C, // STY
        0xAA, 0xA8, 0xBA, 0x8A, 0x9A, 0x98, // TAX, TAY, TSX, TXA, TXS, TYA
    ];

    [TestMethod]
    public void AllOfficialOpcodes_DoNotThrow()
    {
        var seen = new HashSet<byte>();
        foreach (var opcode in OfficialOpcodes)
        {
            if (seen.Contains(opcode))
                continue;
            seen.Add(opcode);

            var ram = new Ram64K();
            ram.WriteByte(0xFFFC, 0x00);
            ram.WriteByte(0xFFFD, 0x80);
            ram.WriteByte(0xFFFE, 0x00);
            ram.WriteByte(0xFFFF, 0x90);
            ram.LoadBytes(0x8000, [opcode, 0x00, 0x00, 0x00, 0x00]);
            var cpu = new Mos6502Cpu(ram);
            cpu.Reset();
            try
            {
                cpu.Step();
            }
            catch (InvalidOperationException ex)
            {
                Assert.Fail($"Official opcode 0x{opcode:X2} threw: {ex.Message}");
            }
        }
    }

    [TestMethod]
    public void Step_ReturnsCycleCountConsistent()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x42, 0x85, 0x10, 0xA5, 0x10, 0xEA }, out _);
        ulong expected = 7; // Reset
        for (int i = 0; i < 5; i++)
        {
            var cycles = cpu.Step();
            expected += (ulong)cycles;
            cpu.CycleCount.Should().Be(expected);
        }
    }
}

[TestClass]
public sealed class Mos6502RegisterTransferTests
{
    private static Mos6502Cpu CreateCpu(byte[] program, out Ram64K ram, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void TAX_TransfersA()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x42, 0xAA }, out _);
        cpu.Step();
        cpu.Step();
        cpu.X.Should().Be(0x42);
    }

    [TestMethod]
    public void TAY_TransfersA()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x55, 0xA8 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.Y.Should().Be(0x55);
    }

    [TestMethod]
    public void TSX_TransfersSP()
    {
        var cpu = CreateCpu(new byte[] { 0xBA }, out _);
        cpu.Step();
        cpu.X.Should().Be(0xFD);
    }

    [TestMethod]
    public void TXA_TransfersX()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x33, 0x8A }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x33);
    }

    [TestMethod]
    public void TXS_TransfersToSP()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x80, 0x9A }, out _);
        cpu.Step();
        cpu.Step();
        cpu.SP.Should().Be(0x80);
    }

    [TestMethod]
    public void TYA_TransfersY()
    {
        var cpu = CreateCpu(new byte[] { 0xA0, 0x77, 0x98 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x77);
    }
}
