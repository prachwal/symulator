using FluentAssertions;
using Symulator.Machines.MinimalBlink.Cpu;
using Symulator.Machines.MinimalBlink.Memory;
using Symulator.Machines.MinimalBlink.Models;
using Symulator.Machines.MinimalBlink.Module;
using Symulator.Machines.MinimalBlink.Programs;

namespace Symulator.Machines.MinimalBlink.Tests;

[TestClass]
public sealed class MinimalBlinkCpuTests
{
    [TestMethod]
    public void Step_Nop_ShouldAdvancePc()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x00);
        cpu.PC = 0x0100;

        cpu.Step();

        cpu.PC.Should().Be(0x0101);
    }

    [TestMethod]
    public void Step_LdaImmediate_ShouldLoadAccumulator()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x01);
        mem.WriteByte(0x0101, 0x42);
        cpu.PC = 0x0100;

        cpu.Step();

        cpu.A.Should().Be(0x42);
        cpu.Z.Should().BeFalse();
        cpu.PC.Should().Be(0x0102);
    }

    [TestMethod]
    public void Step_LdaImmediate_Zero_ShouldSetZ()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x01);
        mem.WriteByte(0x0101, 0x00);
        cpu.PC = 0x0100;

        cpu.Step();

        cpu.A.Should().Be(0x00);
        cpu.Z.Should().BeTrue();
    }

    [TestMethod]
    public void Step_StaAbs_LedPort_ShouldTurnLedOn()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x01);
        mem.WriteByte(0x0101, 0x01);
        mem.WriteByte(0x0102, 0x02);
        mem.WriteByte(0x0103, 0x00);
        mem.WriteByte(0x0104, 0xFF);
        cpu.PC = 0x0100;

        cpu.Step();
        cpu.Step();

        mem.LedState.IsOn.Should().BeTrue();
        mem.LedState.LastValue.Should().Be(0x01);
    }

    [TestMethod]
    public void Step_JmpAbs_ShouldJump()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x03);
        mem.WriteByte(0x0101, 0x34);
        mem.WriteByte(0x0102, 0x12);
        cpu.PC = 0x0100;

        cpu.Step();

        cpu.PC.Should().Be(0x1234);
    }

    [TestMethod]
    public void Step_DecMem_ShouldDecrement()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0000, 5);
        mem.WriteByte(0x0100, 0x05); // DEC_MEM
        mem.WriteByte(0x0101, 0x00);
        mem.WriteByte(0x0102, 0x00);
        cpu.PC = 0x0100;

        cpu.Step();

        mem.GetRamByte(0x0000).Should().Be(4);
        cpu.Z.Should().BeFalse();
    }

    [TestMethod]
    public void Step_JnzAbs_ShouldJumpWhenZIsFalse()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x06); // JNZ_ABS
        mem.WriteByte(0x0101, 0x00);
        mem.WriteByte(0x0102, 0x02);
        cpu.PC = 0x0100;
        cpu.Z = false;

        cpu.Step();

        cpu.PC.Should().Be(0x0200);
    }

    [TestMethod]
    public void Step_Hlt_ShouldSetHalted()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x07);
        cpu.PC = 0x0100;

        cpu.Step();

        cpu.Halted.Should().BeTrue();
    }

    [TestMethod]
    public void Step_UnknownOpcode_ShouldHalt()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0xFF);
        cpu.PC = 0x0100;

        cpu.Step();

        cpu.Halted.Should().BeTrue();
        cpu.LastError.Should().Contain("0xFF");
    }
}

[TestClass]
public sealed class MinimalBlinkPredefinedProgramsTests
{
    [TestMethod]
    public void FindById_LedOn_ShouldReturnProgram()
    {
        var program = MinimalBlinkPredefinedPrograms.FindById("led-on");
        program.Should().NotBeNull();
        program!.Name.Should().Be("LED ON");
    }

    [TestMethod]
    public void PredefinedProgram_LedOn_ShouldTurnLedOnAndHalt()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        var program = MinimalBlinkPredefinedPrograms.FindById("led-on");
        program.Should().NotBeNull();

        mem.Load(program!.LoadAddress, program.Bytes);
        cpu.PC = program.StartAddress;

        for (var i = 0; i < 10 && !cpu.Halted; i++)
            cpu.Step();

        mem.LedState.IsOn.Should().BeTrue();
        cpu.Halted.Should().BeTrue();
    }

    [TestMethod]
    public void PredefinedProgram_BlinkLed_ShouldToggleLed()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        var program = MinimalBlinkPredefinedPrograms.FindById("blink-led");
        program.Should().NotBeNull();

        mem.Load(program!.LoadAddress, program.Bytes);
        cpu.PC = program.StartAddress;

        var initialToggleCount = mem.LedState.ToggleCount;

        for (var i = 0; i < 2000; i++)
            cpu.Step();

        mem.LedState.ToggleCount.Should().BeGreaterThan(initialToggleCount);
    }

    [TestMethod]
    public void FindById_Unknown_ReturnsNull()
    {
        MinimalBlinkPredefinedPrograms.FindById("nope").Should().BeNull();
    }
}

[TestClass]
public sealed class MinimalBlinkMemoryTests
{
    [TestMethod]
    public void WriteReadRam_ShouldRoundtrip()
    {
        var mem = new MinimalBlinkMemory();
        mem.WriteByte(0x0042, 0xAB);

        mem.ReadByte(0x0042).Should().Be(0xAB);
        mem.GetRamByte(0x0042).Should().Be(0xAB);
    }

    [TestMethod]
    public void Read_UnmappedAddress_ReturnsZero()
    {
        var mem = new MinimalBlinkMemory();
        mem.ReadByte(0x0200).Should().Be(0);
    }

    [TestMethod]
    public void Write_LedPort_UpdatesLedState()
    {
        var mem = new MinimalBlinkMemory();
        mem.WriteByte(0xFF00, 0x01);

        mem.LedState.IsOn.Should().BeTrue();
        mem.LedState.LastValue.Should().Be(0x01);
    }

    [TestMethod]
    public void Write_LedPort_ZeroTurnsOff()
    {
        var mem = new MinimalBlinkMemory();
        mem.WriteByte(0xFF00, 0x01);
        mem.WriteByte(0xFF00, 0x00);

        mem.LedState.IsOn.Should().BeFalse();
    }

    [TestMethod]
    public void Load_Rom_ShouldStoreBytes()
    {
        var mem = new MinimalBlinkMemory();
        mem.Load(0x0100, [0x01, 0x02, 0x03]);

        mem.ReadByte(0x0100).Should().Be(0x01);
        mem.ReadByte(0x0101).Should().Be(0x02);
        mem.ReadByte(0x0102).Should().Be(0x03);
    }

    [TestMethod]
    public void ClearAll_ResetsEverything()
    {
        var mem = new MinimalBlinkMemory();
        mem.WriteByte(0x0010, 0xAA);
        mem.WriteByte(0xFF00, 0x01);
        mem.ClearAll();

        mem.GetRamByte(0x0010).Should().Be(0);
        mem.LedState.IsOn.Should().BeFalse();
    }
}

[TestClass]
public sealed class MinimalBlinkSessionTests
{
    [TestMethod]
    public async Task LoadBlinkCommand_ShouldLoadProgramAndSetPc()
    {
        var session = new MinimalBlinkMachineSession();

        var result = await session.ExecuteMachineCommandAsync("minimal-blink.load-blink");

        result.IsSuccess.Should().BeTrue();

        var snapshot = session.Current;
        snapshot.Cpu.Should().NotBeNull();
        snapshot.Cpu!.Pc.Should().Be("$0100");
    }

    [TestMethod]
    public async Task LoadPredefinedProgram_UnknownId_ShouldFail()
    {
        var session = new MinimalBlinkMachineSession();

        var result = await session.ExecuteMachineCommandAsync("minimal-blink.load-predefined-program", "nope");

        result.IsSuccess.Should().BeFalse();
    }

    [TestMethod]
    public async Task Step_AfterReset_ShouldAdvance()
    {
        var session = new MinimalBlinkMachineSession();
        await session.ResetAsync();

        await session.StepInstructionAsync();

        var snapshot = session.Current;
        snapshot.Cpu.Should().NotBeNull();
        snapshot.TotalInstructions.Should().BeGreaterThan(0);
    }
}
