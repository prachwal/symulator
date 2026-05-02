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
