using CmosCpu.Cpu;
using FluentAssertions;
using Moq;

namespace CmosCpu.Cpu.Tests;

[TestClass]
public sealed class Z80CpuTests
{
    [TestMethod]
    public void Reset_ShouldClearRegistersAndCpuState()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        var cpu = new Z80Cpu(memory, io);

        cpu.Registers.A = 0x42;
        cpu.Registers.BC = 0x1234;
        cpu.Registers.PC = 0x8000;

        cpu.Reset();

        cpu.Registers.A.Should().Be(0x00);
        cpu.Registers.BC.Should().Be(0x0000);
        cpu.Registers.PC.Should().Be(0x0000);
        cpu.IsHalted.Should().BeFalse();
        cpu.Iff1.Should().BeFalse();
        cpu.Iff2.Should().BeFalse();
        cpu.InterruptMode.Should().Be(0);
        cpu.CycleCount.Should().Be(0);
    }

    [TestMethod]
    public void Step_Nop_ShouldAdvancePcAndReturnFourCycles()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.WriteByte(0x0000, 0x00);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        result.Opcode.Should().Be(0x00);
        result.Mnemonic.Should().Be("NOP");
        result.Cycles.Should().Be(4);
        result.PcBefore.Should().Be(0x0000);
        cpu.Registers.PC.Should().Be(0x0001);
        cpu.CycleCount.Should().Be(4);
        cpu.IsHalted.Should().BeFalse();
    }

    [TestMethod]
    public void Step_Halt_ShouldSetHalted()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.WriteByte(0x0000, 0x76);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        result.Opcode.Should().Be(0x76);
        result.Mnemonic.Should().Be("HALT");
        result.Cycles.Should().Be(4);
        cpu.IsHalted.Should().BeTrue();
        cpu.Registers.PC.Should().Be(0x0001);
    }

    [TestMethod]
    public void Step_LdAImmediate_ShouldLoadA()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x3E, 0x42]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.A.Should().Be(0x42);
        cpu.Registers.PC.Should().Be(0x0002);
        result.Cycles.Should().Be(7);
        result.Mnemonic.Should().Be("LD A,n");
    }

    [TestMethod]
    public void Step_LdBCImmediate_ShouldLoadBCLittleEndian()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x01, 0x34, 0x12]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.BC.Should().Be(0x1234);
        cpu.Registers.B.Should().Be(0x12);
        cpu.Registers.C.Should().Be(0x34);
        cpu.Registers.PC.Should().Be(0x0003);
        result.Cycles.Should().Be(10);
        result.Mnemonic.Should().Be("LD BC,nn");
    }

    [TestMethod]
    public void Step_LdAFromAddress_ShouldReadMemory()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x3A, 0x00, 0x80]);
        memory.WriteByte(0x8000, 0x5A);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.A.Should().Be(0x5A);
        cpu.Registers.PC.Should().Be(0x0003);
        result.Cycles.Should().Be(13);
        result.Mnemonic.Should().Be("LD A,(nn)");
    }

    [TestMethod]
    public void Step_LdAddressFromA_ShouldWriteMemory()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x32, 0x00, 0x80]);
        var cpu = new Z80Cpu(memory, io);
        cpu.Registers.A = 0xA5;

        Z80StepResult result = cpu.StepDetailed();

        memory.ReadByte(0x8000).Should().Be(0xA5);
        cpu.Registers.PC.Should().Be(0x0003);
        result.Cycles.Should().Be(13);
        result.Mnemonic.Should().Be("LD (nn),A");
    }

    [TestMethod]
    public void Step_Jp_ShouldSetPcToTargetAddress()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0xC3, 0x34, 0x12]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.PC.Should().Be(0x1234);
        result.Cycles.Should().Be(10);
        result.Mnemonic.Should().Be("JP nn");
    }

    [TestMethod]
    public void Step_OutImmediatePortA_ShouldWriteToIoBus()
    {
        var memory = new Ram64K();
        var io = new Mock<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0xD3, 0x01]);
        var cpu = new Z80Cpu(memory, io.Object);
        cpu.Registers.A = 0x41;

        Z80StepResult result = cpu.StepDetailed();

        io.Verify(x => x.WritePort(0x4101, 0x41), Times.Once);
        result.Cycles.Should().Be(11);
        result.Mnemonic.Should().Be("OUT (n),A");
    }

    [TestMethod]
    public void Step_InAImmediatePort_ShouldReadFromIoBus()
    {
        var memory = new Ram64K();
        var io = new Mock<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0xDB, 0x01]);
        io.Setup(x => x.ReadPort(0x1201)).Returns(0x99);
        var cpu = new Z80Cpu(memory, io.Object);
        cpu.Registers.A = 0x12;

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.A.Should().Be(0x99);
        result.Cycles.Should().Be(11);
        result.Mnemonic.Should().Be("IN A,(n)");
    }

    [TestMethod]
    public void Step_UnsupportedOpcode_ShouldThrowInvalidOperationException()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.WriteByte(0x0000, 0xDD);
        var cpu = new Z80Cpu(memory, io);

        Action action = () => cpu.StepDetailed();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*0xDD*");
    }
}
