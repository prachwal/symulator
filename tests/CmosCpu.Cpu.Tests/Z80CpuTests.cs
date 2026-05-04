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

    [TestMethod]
    public void Constructor_NullMemory_ShouldThrow()
    {
        var io = Mock.Of<IZ80IoBus>();

        Action action = () => _ = new Z80Cpu(null!, io);

        action.Should().Throw<ArgumentNullException>().WithParameterName("memory");
    }

    [TestMethod]
    public void Constructor_NullIo_ShouldThrow()
    {
        var memory = new Ram64K();

        Action action = () => _ = new Z80Cpu(memory, null!);

        action.Should().Throw<ArgumentNullException>().WithParameterName("io");
    }

    [TestMethod]
    public void SetProgramCounter_ShouldSetPC()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        var cpu = new Z80Cpu(memory, io);

        cpu.SetProgramCounter(0xABCD);

        cpu.Registers.PC.Should().Be(0xABCD);
    }

    [TestMethod]
    public void Step_ShouldCallStepDetailedAndReturnCycles()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.WriteByte(0x0000, 0x00);
        var cpu = new Z80Cpu(memory, io);

        int cycles = cpu.Step();

        cycles.Should().Be(4);
        cpu.Registers.PC.Should().Be(0x0001);
        cpu.CycleCount.Should().Be(4);
    }

    [TestMethod]
    public void Step_Halted_ShouldStayHaltedAndIncrementR()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.WriteByte(0x0000, 0x76);
        var cpu = new Z80Cpu(memory, io);
        cpu.StepDetailed();

        byte rBefore = cpu.Registers.R;
        Z80StepResult result = cpu.StepDetailed();

        cpu.IsHalted.Should().BeTrue();
        result.Halted.Should().BeTrue();
        result.Opcode.Should().Be(0x76);
        result.Mnemonic.Should().Be("HALT");
        result.Cycles.Should().Be(4);
        result.PcBefore.Should().Be(0x0001);
        cpu.Registers.R.Should().Be((byte)(rBefore + 1));
    }

    [TestMethod]
    public void Step_LdBImmediate_ShouldLoadB()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x06, 0xAB]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.B.Should().Be(0xAB);
        cpu.Registers.PC.Should().Be(0x0002);
        result.Cycles.Should().Be(7);
        result.Mnemonic.Should().Be("LD B,n");
    }

    [TestMethod]
    public void Step_LdCImmediate_ShouldLoadC()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x0E, 0xCD]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.C.Should().Be(0xCD);
        cpu.Registers.PC.Should().Be(0x0002);
        result.Cycles.Should().Be(7);
        result.Mnemonic.Should().Be("LD C,n");
    }

    [TestMethod]
    public void Step_LdDImmediate_ShouldLoadD()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x16, 0x42]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.D.Should().Be(0x42);
        cpu.Registers.PC.Should().Be(0x0002);
        result.Cycles.Should().Be(7);
        result.Mnemonic.Should().Be("LD D,n");
    }

    [TestMethod]
    public void Step_LdEImmediate_ShouldLoadE()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x1E, 0xEF]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.E.Should().Be(0xEF);
        cpu.Registers.PC.Should().Be(0x0002);
        result.Cycles.Should().Be(7);
        result.Mnemonic.Should().Be("LD E,n");
    }

    [TestMethod]
    public void Step_LdHImmediate_ShouldLoadH()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x26, 0x7F]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.H.Should().Be(0x7F);
        cpu.Registers.PC.Should().Be(0x0002);
        result.Cycles.Should().Be(7);
        result.Mnemonic.Should().Be("LD H,n");
    }

    [TestMethod]
    public void Step_LdLImmediate_ShouldLoadL()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x2E, 0x01]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.L.Should().Be(0x01);
        cpu.Registers.PC.Should().Be(0x0002);
        result.Cycles.Should().Be(7);
        result.Mnemonic.Should().Be("LD L,n");
    }

    [TestMethod]
    public void Step_LdDEImmediate_ShouldLoadDELittleEndian()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x11, 0x78, 0x56]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.DE.Should().Be(0x5678);
        cpu.Registers.D.Should().Be(0x56);
        cpu.Registers.E.Should().Be(0x78);
        cpu.Registers.PC.Should().Be(0x0003);
        result.Cycles.Should().Be(10);
        result.Mnemonic.Should().Be("LD DE,nn");
    }

    [TestMethod]
    public void Step_LdHLImmediate_ShouldLoadHLLittleEndian()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x21, 0x34, 0x12]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.HL.Should().Be(0x1234);
        cpu.Registers.H.Should().Be(0x12);
        cpu.Registers.L.Should().Be(0x34);
        cpu.Registers.PC.Should().Be(0x0003);
        result.Cycles.Should().Be(10);
        result.Mnemonic.Should().Be("LD HL,nn");
    }

    [TestMethod]
    public void Step_LdSPImmediate_ShouldLoadSPLittleEndian()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x31, 0x00, 0xFF]);
        var cpu = new Z80Cpu(memory, io);

        Z80StepResult result = cpu.StepDetailed();

        cpu.Registers.SP.Should().Be(0xFF00);
        cpu.Registers.PC.Should().Be(0x0003);
        result.Cycles.Should().Be(10);
        result.Mnemonic.Should().Be("LD SP,nn");
    }

    [TestMethod]
    public void Step_RefreshRegisterShouldIncrementOnEachFetch()
    {
        var memory = new Ram64K();
        var io = Mock.Of<IZ80IoBus>();
        memory.LoadBytes(0x0000, [0x3E, 0x42]);
        var cpu = new Z80Cpu(memory, io);

        cpu.StepDetailed();

        cpu.Registers.R.Should().Be(2);
    }

    [TestMethod]
    public void Registers_AF_ShouldCombineAndSplit()
    {
        var regs = new Z80Registers();

        regs.AF = 0xABCD;

        regs.A.Should().Be(0xAB);
        regs.F.Should().Be(0xCD);
        regs.AF.Should().Be(0xABCD);
    }

    [TestMethod]
    public void Registers_DE_ShouldCombineAndSplit()
    {
        var regs = new Z80Registers();

        regs.DE = 0x1234;

        regs.D.Should().Be(0x12);
        regs.E.Should().Be(0x34);
        regs.DE.Should().Be(0x1234);
    }

    [TestMethod]
    public void Registers_HL_ShouldCombineAndSplit()
    {
        var regs = new Z80Registers();

        regs.HL = 0x5678;

        regs.H.Should().Be(0x56);
        regs.L.Should().Be(0x78);
        regs.HL.Should().Be(0x5678);
    }

    [TestMethod]
    public void Registers_IX_ShouldRoundtrip()
    {
        var regs = new Z80Registers();

        regs.IX = 0xABCD;
        regs.IX.Should().Be(0xABCD);
    }

    [TestMethod]
    public void Registers_IY_ShouldRoundtrip()
    {
        var regs = new Z80Registers();

        regs.IY = 0x1234;
        regs.IY.Should().Be(0x1234);
    }

    [TestMethod]
    public void Registers_SP_ShouldRoundtrip()
    {
        var regs = new Z80Registers();

        regs.SP = 0xFFFE;
        regs.SP.Should().Be(0xFFFE);
    }

    [TestMethod]
    public void Registers_I_ShouldRoundtrip()
    {
        var regs = new Z80Registers();

        regs.I = 0xAB;
        regs.I.Should().Be(0xAB);
    }

    [TestMethod]
    public void Registers_AlternateRegisters_ShouldRoundtrip()
    {
        var regs = new Z80Registers();

        regs.AlternateA = 0xAA;
        regs.AlternateF = 0xBB;
        regs.AlternateB = 0xCC;
        regs.AlternateC = 0xDD;
        regs.AlternateD = 0xEE;
        regs.AlternateE = 0xFF;
        regs.AlternateH = 0x11;
        regs.AlternateL = 0x22;

        regs.AlternateA.Should().Be(0xAA);
        regs.AlternateF.Should().Be(0xBB);
        regs.AlternateB.Should().Be(0xCC);
        regs.AlternateC.Should().Be(0xDD);
        regs.AlternateD.Should().Be(0xEE);
        regs.AlternateE.Should().Be(0xFF);
        regs.AlternateH.Should().Be(0x11);
        regs.AlternateL.Should().Be(0x22);
    }

    [TestMethod]
    public void Registers_ExchangeAF_ShouldSwap()
    {
        var regs = new Z80Registers();
        regs.A = 0x12;
        regs.F = 0x34;
        regs.AlternateA = 0xAB;
        regs.AlternateF = 0xCD;

        regs.ExchangeAF();

        regs.A.Should().Be(0xAB);
        regs.F.Should().Be(0xCD);
        regs.AlternateA.Should().Be(0x12);
        regs.AlternateF.Should().Be(0x34);
    }

    [TestMethod]
    public void Registers_ExchangeGeneralRegisters_ShouldSwap()
    {
        var regs = new Z80Registers();
        regs.B = 0x01;
        regs.C = 0x02;
        regs.D = 0x03;
        regs.E = 0x04;
        regs.H = 0x05;
        regs.L = 0x06;
        regs.AlternateB = 0xF1;
        regs.AlternateC = 0xF2;
        regs.AlternateD = 0xF3;
        regs.AlternateE = 0xF4;
        regs.AlternateH = 0xF5;
        regs.AlternateL = 0xF6;

        regs.ExchangeGeneralRegisters();

        regs.B.Should().Be(0xF1);
        regs.C.Should().Be(0xF2);
        regs.D.Should().Be(0xF3);
        regs.E.Should().Be(0xF4);
        regs.H.Should().Be(0xF5);
        regs.L.Should().Be(0xF6);
        regs.AlternateB.Should().Be(0x01);
        regs.AlternateC.Should().Be(0x02);
        regs.AlternateD.Should().Be(0x03);
        regs.AlternateE.Should().Be(0x04);
        regs.AlternateH.Should().Be(0x05);
        regs.AlternateL.Should().Be(0x06);
    }

    [TestMethod]
    public void Registers_Flags_ShouldSetAndClear()
    {
        var regs = new Z80Registers();

        regs.SignFlag = true;
        regs.SignFlag.Should().BeTrue();
        regs.SignFlag = false;
        regs.SignFlag.Should().BeFalse();

        regs.ZeroFlag = true;
        regs.ZeroFlag.Should().BeTrue();
        regs.ZeroFlag = false;
        regs.ZeroFlag.Should().BeFalse();

        regs.HalfCarryFlag = true;
        regs.HalfCarryFlag.Should().BeTrue();
        regs.HalfCarryFlag = false;
        regs.HalfCarryFlag.Should().BeFalse();

        regs.ParityOverflowFlag = true;
        regs.ParityOverflowFlag.Should().BeTrue();
        regs.ParityOverflowFlag = false;
        regs.ParityOverflowFlag.Should().BeFalse();

        regs.AddSubtractFlag = true;
        regs.AddSubtractFlag.Should().BeTrue();
        regs.AddSubtractFlag = false;
        regs.AddSubtractFlag.Should().BeFalse();

        regs.CarryFlag = true;
        regs.CarryFlag.Should().BeTrue();
        regs.CarryFlag = false;
        regs.CarryFlag.Should().BeFalse();
    }

    [TestMethod]
    public void Registers_Flags_ShouldNotInterfere()
    {
        var regs = new Z80Registers();

        regs.F = 0b_1010_1010;

        regs.SignFlag.Should().BeTrue();
        regs.ZeroFlag.Should().BeFalse();
        regs.HalfCarryFlag.Should().BeFalse();
        regs.ParityOverflowFlag.Should().BeFalse();
        regs.AddSubtractFlag.Should().BeTrue();
        regs.CarryFlag.Should().BeFalse();
    }

    [TestMethod]
    public void Registers_Get8_AllRegisters()
    {
        var regs = new Z80Registers();
        regs.A = 0x01;
        regs.F = 0x02;
        regs.B = 0x03;
        regs.C = 0x04;
        regs.D = 0x05;
        regs.E = 0x06;
        regs.H = 0x07;
        regs.L = 0x08;

        regs.Get8(Z80Register8.A).Should().Be(0x01);
        regs.Get8(Z80Register8.F).Should().Be(0x02);
        regs.Get8(Z80Register8.B).Should().Be(0x03);
        regs.Get8(Z80Register8.C).Should().Be(0x04);
        regs.Get8(Z80Register8.D).Should().Be(0x05);
        regs.Get8(Z80Register8.E).Should().Be(0x06);
        regs.Get8(Z80Register8.H).Should().Be(0x07);
        regs.Get8(Z80Register8.L).Should().Be(0x08);
    }

    [TestMethod]
    public void Registers_Set8_AllRegisters()
    {
        var regs = new Z80Registers();

        regs.Set8(Z80Register8.A, 0xA1);
        regs.Set8(Z80Register8.F, 0xA2);
        regs.Set8(Z80Register8.B, 0xA3);
        regs.Set8(Z80Register8.C, 0xA4);
        regs.Set8(Z80Register8.D, 0xA5);
        regs.Set8(Z80Register8.E, 0xA6);
        regs.Set8(Z80Register8.H, 0xA7);
        regs.Set8(Z80Register8.L, 0xA8);

        regs.A.Should().Be(0xA1);
        regs.F.Should().Be(0xA2);
        regs.B.Should().Be(0xA3);
        regs.C.Should().Be(0xA4);
        regs.D.Should().Be(0xA5);
        regs.E.Should().Be(0xA6);
        regs.H.Should().Be(0xA7);
        regs.L.Should().Be(0xA8);
    }

    [TestMethod]
    public void Registers_Get16_AllRegisters()
    {
        var regs = new Z80Registers();
        regs.AF = 0x0102;
        regs.BC = 0x0304;
        regs.DE = 0x0506;
        regs.HL = 0x0708;
        regs.IX = 0x090A;
        regs.IY = 0x0B0C;
        regs.SP = 0x0D0E;
        regs.PC = 0x0F10;

        regs.Get16(Z80Register16.AF).Should().Be(0x0102);
        regs.Get16(Z80Register16.BC).Should().Be(0x0304);
        regs.Get16(Z80Register16.DE).Should().Be(0x0506);
        regs.Get16(Z80Register16.HL).Should().Be(0x0708);
        regs.Get16(Z80Register16.IX).Should().Be(0x090A);
        regs.Get16(Z80Register16.IY).Should().Be(0x0B0C);
        regs.Get16(Z80Register16.SP).Should().Be(0x0D0E);
        regs.Get16(Z80Register16.PC).Should().Be(0x0F10);
    }

    [TestMethod]
    public void Registers_Set16_AllRegisters()
    {
        var regs = new Z80Registers();

        regs.Set16(Z80Register16.AF, 0x1020);
        regs.Set16(Z80Register16.BC, 0x3040);
        regs.Set16(Z80Register16.DE, 0x5060);
        regs.Set16(Z80Register16.HL, 0x7080);
        regs.Set16(Z80Register16.IX, 0x90A0);
        regs.Set16(Z80Register16.IY, 0xB0C0);
        regs.Set16(Z80Register16.SP, 0xD0E0);
        regs.Set16(Z80Register16.PC, 0xF000);

        regs.AF.Should().Be(0x1020);
        regs.BC.Should().Be(0x3040);
        regs.DE.Should().Be(0x5060);
        regs.HL.Should().Be(0x7080);
        regs.IX.Should().Be(0x90A0);
        regs.IY.Should().Be(0xB0C0);
        regs.SP.Should().Be(0xD0E0);
        regs.PC.Should().Be(0xF000);
    }

    [TestMethod]
    public void Registers_Get8_Invalid_ShouldThrow()
    {
        var regs = new Z80Registers();

        Action action = () => regs.Get8((Z80Register8)0xFF);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Registers_Set8_Invalid_ShouldThrow()
    {
        var regs = new Z80Registers();

        Action action = () => regs.Set8((Z80Register8)0xFF, 0x00);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Registers_Get16_Invalid_ShouldThrow()
    {
        var regs = new Z80Registers();

        Action action = () => regs.Get16((Z80Register16)0xFF);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Registers_Set16_Invalid_ShouldThrow()
    {
        var regs = new Z80Registers();

        Action action = () => regs.Set16((Z80Register16)0xFF, 0x0000);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
