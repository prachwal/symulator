using FluentAssertions;

namespace CmosCpu.Cpu.Tests;

[TestClass]
public sealed class Mos6502HardeningTests
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
    public void StatusByte_ShouldSetUnusedBit_AndUseRequestedStackMarkerBit()
    {
        var cpu = CreateCpu(new byte[] { 0x38 }, out _);

        cpu.Step();

        cpu.GetStatusByte().Should().Be(0x25);
        cpu.GetStatusByte(breakFlag: true).Should().Be(0x35);
    }

    [TestMethod]
    public void PhpPlp_ShouldRoundTripCarryAndInterruptFlags()
    {
        var cpu = CreateCpu(new byte[] { 0x38, 0x08, 0x18, 0x28 }, out var ram);

        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();

        ram.ReadByte(0x01FD).Should().Be(0x35);
        cpu.Carry.Should().BeTrue();
        cpu.InterruptDisable.Should().BeTrue();
        cpu.GetStatusByte().Should().Be(0x25);
    }

    [TestMethod]
    public void Step_ShouldIncludePendingIrqCyclesInReturnedCyclesAndCycleCount()
    {
        var cpu = CreateCpu(new byte[] { 0x58, 0xEA }, out var ram);
        ram.WriteByte(0xFFFE, 0x00);
        ram.WriteByte(0xFFFF, 0x90);

        cpu.Step().Should().Be(2);
        cpu.SetIrqLine(true);

        int cycles = cpu.Step();

        cycles.Should().Be(9);
        cpu.PC.Should().Be(0x9000);
        cpu.InterruptDisable.Should().BeTrue();
        cpu.CycleCount.Should().Be(18UL);
    }

    [TestMethod]
    public void ADC_Decimal_ShouldSetOverflowFromBinaryAddition()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x50, 0x69, 0x50 }, out _);

        cpu.Step();
        cpu.Step();
        cpu.Step();

        cpu.A.Should().Be(0x00);
        cpu.Carry.Should().BeTrue();
        cpu.Zero.Should().BeTrue();
        cpu.Overflow.Should().BeTrue();
    }

    [TestMethod]
    public void SBC_Decimal_ShouldSetOverflowFromBinarySubtraction()
    {
        var cpu = CreateCpu(new byte[] { 0xF8, 0xA9, 0x80, 0x38, 0xE9, 0x01 }, out _);

        cpu.Step();
        cpu.Step();
        cpu.Step();
        cpu.Step();

        cpu.A.Should().Be(0x79);
        cpu.Carry.Should().BeTrue();
        cpu.Overflow.Should().BeTrue();
    }
}
