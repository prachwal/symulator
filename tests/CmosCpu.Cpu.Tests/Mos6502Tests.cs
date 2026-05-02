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

    [TestMethod]
    public void ADC_Binary_01Plus01()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x01, 0x69, 0x01 }, out _);
        cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0x02);
        cpu.Carry.Should().BeFalse();
        cpu.Zero.Should().BeFalse();
        cpu.Negative.Should().BeFalse();
        cpu.Overflow.Should().BeFalse();
    }

    [TestMethod]
    public void ADC_Binary_FFPlus01()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0xFF, 0x69, 0x01 }, out _);
        cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
        cpu.Negative.Should().BeFalse();
        cpu.Overflow.Should().BeFalse();
    }

    [TestMethod]
    public void ADC_Binary_7FPlus01_Overflow()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x7F, 0x69, 0x01 }, out _);
        cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0x80);
        cpu.Carry.Should().BeFalse();
        cpu.Zero.Should().BeFalse();
        cpu.Negative.Should().BeTrue();
        cpu.Overflow.Should().BeTrue();
    }

    [TestMethod]
    public void ADC_Binary_80Plus80_Overflow()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x80, 0x69, 0x80 }, out _);
        cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
        cpu.Negative.Should().BeFalse();
        cpu.Overflow.Should().BeTrue();
    }

    [TestMethod]
    public void SBC_Binary_05Minus03()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x05, 0x38, 0xE9, 0x03 }, out _);
        cpu.Step(); cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0x02);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeFalse();
        cpu.Negative.Should().BeFalse();
        cpu.Overflow.Should().BeFalse();
    }

    [TestMethod]
    public void SBC_Binary_00Minus01_Borrow()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x00, 0x38, 0xE9, 0x01 }, out _);
        cpu.Step(); cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0xFF);
        cpu.Carry.Should().BeFalse();
        cpu.Zero.Should().BeFalse();
        cpu.Negative.Should().BeTrue();
        cpu.Overflow.Should().BeFalse();
    }

    [TestMethod]
    public void SBC_Binary_80Minus01_Overflow()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x80, 0x38, 0xE9, 0x01 }, out _);
        cpu.Step(); cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0x7F);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeFalse();
        cpu.Negative.Should().BeFalse();
        cpu.Overflow.Should().BeTrue();
    }

    [TestMethod]
    public void SBC_Binary_7FMinusFF_Overflow()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x7F, 0x38, 0xE9, 0xFF }, out _);
        cpu.Step(); cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0x80);
        cpu.Carry.Should().BeFalse();
        cpu.Zero.Should().BeFalse();
        cpu.Negative.Should().BeTrue();
        cpu.Overflow.Should().BeTrue();
    }

    [TestMethod]
    public void SBC_Decimal_10Minus01()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x10, 0x38, 0xE9, 0x01 }, out _);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0x09);
        cpu.Carry.Should().BeTrue();
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

    [TestMethod]
    public void ASL_ZeroPage_WritesToCorrectAddress()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x01, 0x85, 0x10, 0x06, 0x10 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step();
        cpu.A.Should().Be(0x01);
        ram.ReadByte(0x0010).Should().Be(0x02);
        cpu.Carry.Should().BeFalse();
        cpu.Zero.Should().BeFalse();
        cpu.Negative.Should().BeFalse();
    }

    [TestMethod]
    public void ASL_ZeroPage_AdvancesPC()
    {
        var cpu = CreateCpu(new byte[] { 0x06, 0x10, 0xA9, 0x42 }, out _);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x42);
    }

    [TestMethod]
    public void ASL_ZeroPage_SetsCarryAndZero()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x80, 0x85, 0x10, 0x06, 0x10 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x0010).Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void ASL_ZeroPageX_WrapsInZeroPage()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0xFF, 0xA9, 0x01, 0x85, 0x00, 0x16, 0x01 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x0000).Should().Be(0x02);
    }

    [TestMethod]
    public void ASL_Absolute_WritesCorrectAddress()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x40, 0x8D, 0x00, 0x90, 0x0E, 0x00, 0x90 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x9000).Should().Be(0x80);
        cpu.Carry.Should().BeFalse();
        cpu.Negative.Should().BeTrue();
    }

    [TestMethod]
    public void ASL_AbsoluteX_WritesCorrectAddress()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x05, 0xA9, 0x01, 0x9D, 0x00, 0x90, 0x1E, 0x00, 0x90 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x9005).Should().Be(0x02);
    }

    [TestMethod]
    public void LSR_ZeroPage_WritesCorrectAddress()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x04, 0x85, 0x10, 0x46, 0x10 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x0010).Should().Be(0x02);
    }

    [TestMethod]
    public void LSR_ZeroPage_SetsCarry()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x01, 0x85, 0x10, 0x46, 0x10 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x0010).Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void LSR_Absolute_WritesCorrectAddress()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x08, 0x8D, 0x00, 0x90, 0x4E, 0x00, 0x90 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x9000).Should().Be(0x04);
    }

    [TestMethod]
    public void LSR_AbsoluteX_WritesCorrectAddress()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x03, 0xA9, 0x10, 0x9D, 0x00, 0x90, 0x5E, 0x00, 0x90 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x9003).Should().Be(0x08);
    }

    [TestMethod]
    public void ROL_ZeroPage_WithCarryIn()
    {
        var cpu = CreateCpu(new byte[] { 0x38, 0xA9, 0x40, 0x85, 0x10, 0x26, 0x10 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x0010).Should().Be(0x81);
        cpu.Carry.Should().BeFalse();
        cpu.Negative.Should().BeTrue();
    }

    [TestMethod]
    public void ROL_ZeroPage_SetsCarry()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x80, 0x85, 0x10, 0x26, 0x10 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x0010).Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
    }

    [TestMethod]
    public void ROR_ZeroPage_WithCarryIn()
    {
        var cpu = CreateCpu(new byte[] { 0x38, 0xA9, 0x01, 0x85, 0x10, 0x66, 0x10 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x0010).Should().Be(0x80);
        cpu.Carry.Should().BeTrue();
        cpu.Negative.Should().BeTrue();
    }

    [TestMethod]
    public void ROR_Absolute_WritesCorrectAddress()
    {
        var cpu = CreateCpu(new byte[] { 0xA9, 0x02, 0x8D, 0x00, 0x90, 0x6E, 0x00, 0x90 }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x9000).Should().Be(0x01);
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

    // This test verifies dispatcher coverage only.
    // Semantic correctness is covered by dedicated opcode/addressing-mode tests.
    [TestMethod]
    public void AllOfficialOpcodes_AreRecognizedByDispatcher()
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

[TestClass]
public sealed class Mos6502AddressingModeTests
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
    public void ZeroPageWrap_LDA_ffX_withX1_readsFrom00()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x01, 0xB5, 0xFF }, out var ram);
        ram.WriteByte(0x0000, 0x42);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x42);
    }

    [TestMethod]
    public void ZeroPageWrap_STA_ffX_withX1_writesTo00()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x01, 0xA9, 0xAB, 0x95, 0xFF }, out var ram);
        cpu.Step(); cpu.Step(); cpu.Step();
        ram.ReadByte(0x0000).Should().Be(0xAB);
    }

    [TestMethod]
    public void ZeroPageWrap_LDX_ffY_withY1_readsFrom00()
    {
        var cpu = CreateCpu(new byte[] { 0xA0, 0x01, 0xB6, 0xFF }, out var ram);
        ram.WriteByte(0x0000, 0x77);
        cpu.Step();
        cpu.Step();
        cpu.X.Should().Be(0x77);
    }

    [TestMethod]
    public void IndexedIndirect_WrapsInZeroPage()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x01, 0xA1, 0xFF }, out var ram);
        // X=1, operand=$FF → (operand+X) = $00
        // [$00] = $34, [$01] = $12 → final address $1234
        ram.WriteByte(0x0000, 0x34);
        ram.WriteByte(0x0001, 0x12);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(ram.ReadByte(0x1234));
    }

    [TestMethod]
    public void IndexedIndirect_HighByteWrapsInZeroPage()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x01, 0xA1, 0xFF }, out var ram);
        ram.WriteByte(0x0000, 0x34);
        ram.WriteByte(0x0001, 0x12);
        ram.WriteByte(0x1234, 0xAA);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0xAA);
    }

    [TestMethod]
    public void IndirectIndexed_ReadsBaseFromZeroPage()
    {
        var cpu = CreateCpu(new byte[] { 0xA0, 0x05, 0xB1, 0x10 }, out var ram);
        ram.WriteByte(0x0010, 0x00);
        ram.WriteByte(0x0011, 0x90);
        ram.WriteByte(0x9005, 0x42);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x42);
    }

    [TestMethod]
    public void IndirectIndexed_HighByteWrapsAtFF()
    {
        var cpu = CreateCpu(new byte[] { 0xA0, 0x00, 0xB1, 0xFF }, out var ram);
        ram.WriteByte(0x00FF, 0x00);
        ram.WriteByte(0x0000, 0x90);
        ram.WriteByte(0x9000, 0x55);
        cpu.Step();
        cpu.Step();
        cpu.A.Should().Be(0x55);
    }

    [TestMethod]
    public void IndirectIndexed_PageCross_AddsCycle()
    {
        var cpu = CreateCpu(new byte[] { 0xA0, 0x01, 0xB1, 0x10 }, out var ram);
        ram.WriteByte(0x0010, 0xFF);
        ram.WriteByte(0x0011, 0x80);
        cpu.Step();
        var cycles = cpu.Step();
        cycles.Should().Be(6); // 5 base + 1 page cross
    }

    [TestMethod]
    public void IndirectIndexed_SamePage_NoExtraCycle()
    {
        var cpu = CreateCpu(new byte[] { 0xA0, 0x00, 0xB1, 0x10 }, out var ram);
        ram.WriteByte(0x0010, 0x00);
        ram.WriteByte(0x0011, 0x80);
        cpu.Step();
        var cycles = cpu.Step();
        cycles.Should().Be(5);
    }

    [TestMethod]
    public void JMP_Indirect_PageWrapBug_HighByteFromSamePage()
    {
        var ram = new Ram64K();
        ram.WriteByte(0xFFFC, 0x00);
        ram.WriteByte(0xFFFD, 0x80);
        // JMP ($12FF) at 0x8000
        ram.WriteByte(0x8000, 0x6C);
        ram.WriteByte(0x8001, 0xFF);
        ram.WriteByte(0x8002, 0x12);
        // low byte at $12FF
        ram.WriteByte(0x12FF, 0x34);
        // high byte would be at $1300 on a correct CPU, but on NMOS 6502 reads $1200
        ram.WriteByte(0x1300, 0xAA);
        ram.WriteByte(0x1200, 0x12); // bug: high byte from $1200, not $1300
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        cpu.Step();
        cpu.PC.Should().Be(0x1234);
    }
}

[TestClass]
public sealed class Mos6502CycleTests
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
    public void Reset_SetsCycleCountTo7()
    {
        var cpu = CreateCpu(new byte[] { 0xEA }, out _);
        cpu.CycleCount.Should().Be(7);
    }

    [TestMethod]
    public void NOP_IncreasesCycleCountBy2()
    {
        var cpu = CreateCpu(new byte[] { 0xEA }, out _);
        cpu.Step();
        cpu.CycleCount.Should().Be(9); // 7 reset + 2
    }

    [TestMethod]
    public void LDA_AbsoluteX_PageCross_AddsCycle()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x01, 0xBD, 0xFF, 0x80 }, out var ram);
        // 0x80FF + 1 = 0x8100 → page crossed
        ram.WriteByte(0x8100, 0x42);
        cpu.Step();
        var cycles = cpu.Step();
        cycles.Should().Be(5); // 4 + 1 page cross
    }

    [TestMethod]
    public void LDA_AbsoluteX_NoPageCross_NominalCycles()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x01, 0xBD, 0x00, 0x80 }, out var ram);
        // 0x8000 + 1 = 0x8001 → same page
        ram.WriteByte(0x8001, 0x42);
        cpu.Step();
        var cycles = cpu.Step();
        cycles.Should().Be(4);
    }

    [TestMethod]
    public void STA_AbsoluteX_Always5Cycles()
    {
        var cpu = CreateCpu(new byte[] { 0xA2, 0x01, 0xA9, 0x42, 0x9D, 0xFF, 0x80 }, out _);
        cpu.Step(); cpu.Step();
        // STA $80FF,X with X=1 → no page cross issue for writes
        var cycles = cpu.Step();
        cycles.Should().Be(5);
    }

    [TestMethod]
    public void LDA_ZeroPage_Returns3Cycles()
    {
        var cpu = CreateCpu(new byte[] { 0xA5, 0x10 }, out _);
        var cycles = cpu.Step();
        cycles.Should().Be(3);
    }

    [TestMethod]
    public void LDA_Absolute_Returns4Cycles()
    {
        var cpu = CreateCpu(new byte[] { 0xAD, 0x00, 0x90 }, out _);
        var cycles = cpu.Step();
        cycles.Should().Be(4);
    }
}

[TestClass]
public sealed class Mos6502BcdMatrixTests
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

    private static byte ToBcd(int value)
    {
        int tens = value / 10;
        int ones = value % 10;
        return (byte)((tens << 4) | ones);
    }

    private static int FromBcd(byte value)
    {
        return ((value >> 4) & 0x0F) * 10 + (value & 0x0F);
    }

    [TestMethod]
    public void ADC_DecimalMatrix_AllPairs()
    {
        var pairs = new (int a, int b, int expectedSum, bool expectCarry)[]
        {
            (00, 00, 00, false),
            (00, 01, 01, false),
            (01, 09, 10, false),
            (09, 01, 10, false),
            (10, 01, 11, false),
            (15, 27, 42, false),
            (49, 50, 99, false),
            (50, 50, 00, true),
            (99, 01, 00, true),
            (99, 99, 98, true),
        };

        foreach (var (a, b, expectedSum, expectCarry) in pairs)
        {
            byte aBcd = ToBcd(a);
            byte bBcd = ToBcd(b);
            byte expectedBcd = ToBcd(expectedSum);

            var cpu = CreateCpu(
            [
                0xF8,       // SED
                0xA9, aBcd, // LDA #aBcd
                0x69, bBcd, // ADC #bBcd
            ], out _);

            cpu.Step(); // SED
            cpu.Step(); // LDA
            cpu.Step(); // ADC

            cpu.A.Should().Be(expectedBcd,
                $"ADC BCD {a}+{b}: expected 0x{expectedBcd:X2}, got 0x{cpu.A:X2}");
            cpu.Carry.Should().Be(expectCarry,
                $"ADC BCD {a}+{b}: carry mismatch");
            cpu.Zero.Should().Be(expectedSum == 0,
                $"ADC BCD {a}+{b}: zero flag mismatch");
            cpu.Negative.Should().Be((expectedBcd & 0x80) != 0,
                $"ADC BCD {a}+{b}: negative flag mismatch");
        }
    }

    [TestMethod]
    public void SBC_DecimalMatrix_AllPairs()
    {
        var pairs = new (int a, int b, int expectedDiff, bool expectCarry)[]
        {
            (00, 00, 00, true),
            (01, 00, 01, true),
            (10, 01, 09, true),
            (42, 27, 15, true),
            (50, 49, 01, true),
            (99, 01, 98, true),
            (00, 01, 99, false),
            (10, 11, 99, false),
            (99, 99, 00, true),
        };

        foreach (var (a, b, expectedDiff, expectCarry) in pairs)
        {
            byte aBcd = ToBcd(a);
            byte bBcd = ToBcd(b);
            byte expectedBcd = ToBcd(expectedDiff);

            var cpu = CreateCpu(
            [
                0xF8,       // SED
                0xA9, aBcd, // LDA #aBcd
                0x38,       // SEC (no borrow)
                0xE9, bBcd, // SBC #bBcd
            ], out _);

            cpu.Step(); // SED
            cpu.Step(); // LDA
            cpu.Step(); // SEC
            cpu.Step(); // SBC

            cpu.A.Should().Be(expectedBcd,
                $"SBC BCD {a}-{b}: expected 0x{expectedBcd:X2}, got 0x{cpu.A:X2}");
            cpu.Carry.Should().Be(expectCarry,
                $"SBC BCD {a}-{b}: carry mismatch");
            cpu.Zero.Should().Be(expectedDiff == 0,
                $"SBC BCD {a}-{b}: zero flag mismatch");
            cpu.Negative.Should().Be((expectedBcd & 0x80) != 0,
                $"SBC BCD {a}-{b}: negative flag mismatch");
        }
    }
}

[TestClass]
public sealed class Mos6502InterruptDetailTests
{
    private static Mos6502Cpu SetupForInterrupts(byte[] program, out Ram64K ram,
        ushort irqVector = 0x9000, ushort nmiVector = 0x9000, ushort origin = 0x8000)
    {
        ram = new Ram64K();
        ram.WriteByte(0xFFFC, (byte)(origin & 0xFF));
        ram.WriteByte(0xFFFD, (byte)((origin >> 8) & 0xFF));
        ram.WriteByte(0xFFFE, (byte)(irqVector & 0xFF));
        ram.WriteByte(0xFFFF, (byte)((irqVector >> 8) & 0xFF));
        ram.WriteByte(0xFFFA, (byte)(nmiVector & 0xFF));
        ram.WriteByte(0xFFFB, (byte)((nmiVector >> 8) & 0xFF));
        ram.LoadBytes(origin, program);
        if (irqVector >= 0x8000)
            ram.LoadBytes(irqVector, [0x40]); // RTI at IRQ vector
        if (nmiVector >= 0x8000 && nmiVector != irqVector)
            ram.LoadBytes(nmiVector, [0x40]); // RTI at NMI vector
        var cpu = new Mos6502Cpu(ram);
        cpu.Reset();
        return cpu;
    }

    [TestMethod]
    public void BRK_LandsAtIrqVector()
    {
        var cpu = SetupForInterrupts(new byte[] { 0x00, 0x00, 0xEA }, out _);
        cpu.Step();
        cpu.PC.Should().Be(0x9000);
        cpu.InterruptDisable.Should().BeTrue();
    }

    [TestMethod]
    public void BRK_DecrementsSPBy3()
    {
        var cpu = SetupForInterrupts(new byte[] { 0x00, 0x00 }, out _);
        cpu.Step();
        cpu.SP.Should().Be(0xFA); // 0xFD - 3 (2 for PC, 1 for status)
    }

    [TestMethod]
    public void BRK_SetsInterruptDisable()
    {
        var cpu = SetupForInterrupts(new byte[] { 0x58, 0x00, 0x00 }, out _);
        cpu.Step(); // CLI
        cpu.InterruptDisable.Should().BeFalse();
        cpu.Step(); // BRK
        cpu.InterruptDisable.Should().BeTrue();
    }

    [TestMethod]
    public void IRQ_SetsInterruptDisableAndPushesPC()
    {
        var cpu = SetupForInterrupts(new byte[] { 0x58, 0xEA }, out _);
        cpu.Step(); // CLI
        cpu.Irq();
        cpu.PC.Should().Be(0x9000);
        cpu.InterruptDisable.Should().BeTrue();
        cpu.SP.Should().Be(0xFA); // pushed PC (2) + status (1)
    }

    [TestMethod]
    public void IRQ_IgnoredWhenInterruptDisable()
    {
        var cpu = SetupForInterrupts(new byte[] { 0xEA }, out _);
        cpu.Irq();
        cpu.PC.Should().Be(0x8000); // unchanged
        cpu.SP.Should().Be(0xFD); // unchanged
    }

    [TestMethod]
    public void NMI_WorksEvenWhenInterruptDisabled()
    {
        var cpu = SetupForInterrupts(new byte[] { 0x78, 0xEA }, out _,
            nmiVector: 0x9000);
        cpu.Nmi();
        cpu.PC.Should().Be(0x9000);
        cpu.InterruptDisable.Should().BeTrue();
        cpu.SP.Should().Be(0xFA);
    }

    [TestMethod]
    public void NMI_UsesCorrectVector()
    {
        var cpu = SetupForInterrupts(new byte[] { 0xEA }, out var ram,
            irqVector: 0x9000, nmiVector: 0x9005);
        ram.WriteByte(0x9005, 0xEA); // NOP at NMI vector
        cpu.Nmi();
        cpu.PC.Should().Be(0x9005);
    }

    [TestMethod]
    public void RTI_RestoresPC()
    {
        var cpu = SetupForInterrupts(new byte[] { 0x58, 0xEA }, out _);
        cpu.Step(); // CLI
        cpu.Irq(); // pushes PC=0x8001, status → jumps to 0x9000
        // 0x9000 has RTI (0x40) from SetupForInterrupts
        // RTI in original program at 0x8001 is 0xEA (NOP), not used
        cpu.PC.Should().Be(0x9000); // IRQ took us here
        // Now step: RTI at 0x9000 should restore PC to 0x8001
        cpu.Step();
        cpu.PC.Should().Be(0x8001);
    }

    [TestMethod]
    public void RTI_RestoresStatus()
    {
        var cpu = SetupForInterrupts(new byte[] { 0x38, 0xF8, 0xEA }, out _);
        cpu.Step(); // SEC → C=1
        cpu.Step(); // SED → D=1
        // Now status has C=1, D=1, I=1 (from reset)
        cpu.Irq(); // pushes status with B=0, I=1, C=1, D=1
        cpu.Step(); // RTI at 0x9000 should restore
        cpu.Carry.Should().BeTrue();
        cpu.Decimal.Should().BeTrue();
        cpu.PC.Should().Be(0x8003);
    }

    [TestMethod]
    public void IRQ_BreakFlagNotSet()
    {
        var cpu = SetupForInterrupts(new byte[] { 0x58, 0xEA }, out var ram);
        cpu.Step(); // CLI
        // Manually inspect pushed status byte on stack
        // Before IRQ: SP=0xFC (after one step)
        byte spBefore = cpu.SP;
        cpu.Irq();
        byte spAfter = cpu.SP;
        // IRQ pushed PCH, PCL, status (3 bytes)
        // The status byte is at 0x0100 + spAfter + 1 (since SP decremented after push)
        // Actually Push decrements SP after write:
        // Write at 0x0100+SP, then SP--
        // So for push1: SP=SP_before, write at 0x0100+SP_before, SP--
        // for push3: status byte is at 0x0100 + (spBefore - 2)
        byte pushedStatus = ram.ReadByte((ushort)(0x0100 + spBefore - 2));
        // Break flag (bit 4) should NOT be set in IRQ
        (pushedStatus & 0x10).Should().Be(0, "IRQ must not set break flag in pushed status");
        // Bit 5 should be set (reserved)
        (pushedStatus & 0x20).Should().Be(0x20, "IRQ pushed status must have bit 5 set");
    }
}
