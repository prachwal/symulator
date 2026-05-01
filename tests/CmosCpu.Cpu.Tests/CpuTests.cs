using CmosCpu.Core;
using CmosCpu.Cpu;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CmosCpu.Cpu.Tests;

[TestClass]
public class CpuTests
{
    private Mock<IBus> _busMock = null!;
    private CpuCore _cpu = null!;

    [TestInitialize]
    public void Setup()
    {
        _busMock = new Mock<IBus>();
        // Default: RESET vector at 0xFFFC points to 0x8000
        _busMock.Setup(b => b.Read(0xFFFC)).Returns((byte)0x00);
        _busMock.Setup(b => b.Read(0xFFFD)).Returns((byte)0x80);
        _cpu = new CpuCore(_busMock.Object);
    }

    [TestMethod]
    public void Reset_SetsPCFromResetVector()
    {
        _cpu.Reset();
        _cpu.Registers.PC.Should().Be(0x8000);
    }

    [TestMethod]
    public void Reset_SetsFlags()
    {
        _cpu.Reset();
        _cpu.Registers.InterruptDisableFlag.Should().BeTrue();
        _cpu.Registers.SP.Should().Be(0xFF);
        _cpu.Registers.Halted.Should().BeFalse();
    }

    [TestMethod]
    public void LDA_IMM_LoadsValue()
    {
        SetupMemoryAt(0x8000, 0x01, 0x42);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Registers.A.Should().Be(0x42);
        _cpu.Registers.PC.Should().Be(0x8002);
    }

    [TestMethod]
    public void LDA_IMM_SetsZeroFlag()
    {
        SetupMemoryAt(0x8000, 0x01, 0x00);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Registers.A.Should().Be(0);
        _cpu.Registers.ZeroFlag.Should().BeTrue();
    }

    [TestMethod]
    public void LDA_IMM_SetsNegativeFlag()
    {
        SetupMemoryAt(0x8000, 0x01, 0x80);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Registers.A.Should().Be(0x80);
        _cpu.Registers.NegativeFlag.Should().BeTrue();
    }

    [TestMethod]
    public void LDA_ABS_LoadsFromAddress()
    {
        SetupMemoryAt(0x8000, 0x02, 0x34, 0x12);
        _busMock.Setup(b => b.Read(0x1234)).Returns(0xAB);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Registers.A.Should().Be(0xAB);
        _cpu.Registers.PC.Should().Be(0x8003);
    }

    [TestMethod]
    public void STA_ABS_StoresToAddress()
    {
        SetupMemoryAt(0x8000, 0x01, 0x42);
        SetupMemoryAt(0x8002, 0x03, 0x00, 0x20);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Step();
        _busMock.Verify(b => b.Write(0x2000, 0x42), Times.Once);
    }

    [TestMethod]
    public void ADD_IMM_AddsToA()
    {
        SetupMemoryAt(0x8000, 0x01, 0x10);
        SetupMemoryAt(0x8002, 0x04, 0x20);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Step();
        _cpu.Registers.A.Should().Be(0x30);
        _cpu.Registers.ZeroFlag.Should().BeFalse();
    }

    [TestMethod]
    public void ADD_IMM_SetsCarryFlag()
    {
        SetupMemoryAt(0x8000, 0x01, 0xFF);
        SetupMemoryAt(0x8002, 0x04, 0x01);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Step();
        _cpu.Registers.A.Should().Be(0x00);
        _cpu.Registers.ZeroFlag.Should().BeTrue();
        _cpu.Registers.CarryFlag.Should().BeTrue();
    }

    [TestMethod]
    public void SUB_IMM_SubtractsFromA()
    {
        SetupMemoryAt(0x8000, 0x01, 0x50);
        SetupMemoryAt(0x8002, 0x05, 0x30);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Step();
        _cpu.Registers.A.Should().Be(0x20);
        _cpu.Registers.CarryFlag.Should().BeTrue();
    }

    [TestMethod]
    public void JMP_JumpsToAddress()
    {
        SetupMemoryAt(0x8000, 0x06, 0x00, 0x90);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Registers.PC.Should().Be(0x9000);
    }

    [TestMethod]
    public void JZ_JumpsWhenZeroFlagSet()
    {
        SetupMemoryAt(0x8000, 0x01, 0x00);
        SetupMemoryAt(0x8002, 0x07, 0x00, 0x90);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Step();
        _cpu.Registers.PC.Should().Be(0x9000);
    }

    [TestMethod]
    public void JZ_DoesNotJumpWhenZeroFlagNotSet()
    {
        SetupMemoryAt(0x8000, 0x01, 0x01);
        SetupMemoryAt(0x8002, 0x07, 0x00, 0x90);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Step();
        _cpu.Registers.PC.Should().Be(0x8005);
    }

    [TestMethod]
    public void JNZ_JumpsWhenZeroFlagNotSet()
    {
        SetupMemoryAt(0x8000, 0x01, 0x01);
        SetupMemoryAt(0x8002, 0x08, 0x00, 0x90);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Step();
        _cpu.Registers.PC.Should().Be(0x9000);
    }

    [TestMethod]
    public void JNZ_DoesNotJumpWhenZeroFlagSet()
    {
        SetupMemoryAt(0x8000, 0x01, 0x00);
        SetupMemoryAt(0x8002, 0x08, 0x00, 0x90);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Step();
        _cpu.Registers.PC.Should().Be(0x8005);
    }

    [TestMethod]
    public void CALL_PushesReturnAddressAndJumps()
    {
        SetupMemoryAt(0x8000, 0x0F, 0x00, 0x90);
        _busMock.Setup(b => b.Read(0x01FF)).Returns(0x00);
        _busMock.Setup(b => b.Read(0x01FE)).Returns(0x00);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Registers.PC.Should().Be(0x9000);
        _busMock.Verify(b => b.Write(0x01FF, 0x80), Times.Once);
        _busMock.Verify(b => b.Write(0x01FE, 0x03), Times.Once);
        _cpu.Registers.SP.Should().Be(0xFD);
    }

    [TestMethod]
    public void RET_PopsAddressAndReturns()
    {
        _busMock.Setup(b => b.Read(0x01FE)).Returns(0x00);
        _busMock.Setup(b => b.Read(0x01FF)).Returns(0x90);
        _cpu.Reset();
        _cpu.Registers.SP = 0xFD;

        SetupMemoryAt(0x8000, 0x10);
        _cpu.Step();
        _cpu.Registers.PC.Should().Be(0x9000);
        _cpu.Registers.SP.Should().Be(0xFF);
    }

    [TestMethod]
    public void HLT_HaltsCPU()
    {
        SetupMemoryAt(0x8000, 0x11);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Registers.Halted.Should().BeTrue();
    }

    [TestMethod]
    public void HLT_StepDoesNothingWhenHalted()
    {
        SetupMemoryAt(0x8000, 0x11);
        _cpu.Reset();
        _cpu.Step();
        ushort pcAfterHalt = _cpu.Registers.PC;
        _cpu.Step();
        _cpu.Registers.PC.Should().Be(pcAfterHalt);
        _cpu.Registers.Halted.Should().BeTrue();
    }

    [TestMethod]
    public void Run_ExecutesMultipleSteps()
    {
        SetupMemoryAt(0x8000, 0x01, 0x42);
        SetupMemoryAt(0x8002, 0x01, 0x24);
        _cpu.Reset();
        _cpu.Run(10);
        _cpu.Registers.A.Should().Be(0x24);
    }

    [TestMethod]
    public void CLI_ClearsInterruptDisable()
    {
        SetupMemoryAt(0x8000, 0x0B);
        _cpu.Reset();
        _cpu.Registers.InterruptDisableFlag = true;
        _cpu.Step();
        _cpu.Registers.InterruptDisableFlag.Should().BeFalse();
    }

    [TestMethod]
    public void SEI_SetsInterruptDisable()
    {
        SetupMemoryAt(0x8000, 0x0C);
        _cpu.Reset();
        _cpu.Registers.InterruptDisableFlag = false;
        _cpu.Step();
        _cpu.Registers.InterruptDisableFlag.Should().BeTrue();
    }

    [TestMethod]
    public void OUT_WritesAToBus()
    {
        SetupMemoryAt(0x8000, 0x01, 0x55);
        SetupMemoryAt(0x8002, 0x09, 0x00);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Step();
        _busMock.Verify(b => b.Write(0xC000, 0x55), Times.Once);
    }

    [TestMethod]
    public void CycleCount_Increments()
    {
        SetupMemoryAt(0x8000, 0x01, 0x42);
        _cpu.Reset();
        _cpu.Registers.CycleCount.Should().Be(0);
        _cpu.Step();
        _cpu.Registers.CycleCount.Should().Be(2);
    }

    [TestMethod]
    public void NOP_IncrementsPC()
    {
        SetupMemoryAt(0x8000, 0x00);
        _cpu.Reset();
        _cpu.Step();
        _cpu.Registers.PC.Should().Be(0x8001);
    }

    private void SetupMemoryAt(ushort address, params byte[] bytes)
    {
        for (int i = 0; i < bytes.Length; i++)
        {
            ushort addr = (ushort)(address + i);
            _busMock.Setup(b => b.Read(addr)).Returns(bytes[i]);
        }
    }
}
