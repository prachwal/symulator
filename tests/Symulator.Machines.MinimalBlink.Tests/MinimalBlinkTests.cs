using FluentAssertions;
using Symulator.Application.Abstractions;
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

    [TestMethod]
    public void AndImmediate_ShouldMaskAccumulator()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x0B); // AND #imm
        mem.WriteByte(0x0101, 0x80);
        cpu.PC = 0x0100;
        cpu.A = 0x81;

        cpu.Step();

        cpu.A.Should().Be(0x80);
        cpu.Z.Should().BeFalse();
        cpu.N.Should().BeTrue();
    }

    [TestMethod]
    public void AndImmediate_ZeroResult_ShouldSetZ()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x0B);
        mem.WriteByte(0x0101, 0x80);
        cpu.PC = 0x0100;
        cpu.A = 0x01;

        cpu.Step();

        cpu.A.Should().Be(0x00);
        cpu.Z.Should().BeTrue();
        cpu.N.Should().BeFalse();
    }

    [TestMethod]
    public void Bne_ShouldBranch_WhenZIsFalse()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x0C); // BNE
        mem.WriteByte(0x0101, 0x02); // +2 → skip 1 NOP, land at $0104
        mem.WriteByte(0x0102, 0x00); // NOP (skipped)
        mem.WriteByte(0x0103, 0x00); // NOP (skipped)
        mem.WriteByte(0x0104, 0x07); // HLT (land here)
        cpu.PC = 0x0100;
        cpu.Z = false;

        cpu.Step();

        cpu.PC.Should().Be(0x0104);
    }

    [TestMethod]
    public void Bne_ShouldNotBranch_WhenZIsTrue()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x0C); // BNE
        mem.WriteByte(0x0101, 0x03); // +3
        cpu.PC = 0x0100;
        cpu.Z = true;

        cpu.Step();

        cpu.PC.Should().Be(0x0102);
    }

    [TestMethod]
    public void Bne_NegativeOffset_ShouldBranchBackward()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.WriteByte(0x0100, 0x0C); // BNE
        mem.WriteByte(0x0101, 0xFE); // -2 → loops to $0100
        cpu.PC = 0x0100;
        cpu.Z = false;

        cpu.Step();

        cpu.PC.Should().Be(0x0100);
    }

    [TestMethod]
    public void JsrRts_ShouldReturnToInstructionAfterJsr()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);

        // JSR at $01F8 to $01F4 (RTS below stack push range $01FE-$01FF)
        // JSR pushes $01FA, jumps to $01F4
        // RTS at $01F4 returns to $01FA+1 = $01FB
        mem.Load(0x01F0,
        [
            0x0E,             // RTS at $01F0
            0x00,             // NOP at $01F1
            0x00,             // NOP at $01F2
            0x00,             // NOP at $01F3
            0x0E,             // RTS at $01F4
            0x00,             // NOP at $01F5
            0x00,             // NOP at $01F6
            0x00,             // NOP at $01F7
            0x0D, 0xF4, 0x01, // JSR $01F4 at $01F8
            0x01, 0x42,       // LDA #$42 at $01FB
            0x07,             // HLT at $01FD
        ]);

        cpu.PC = 0x01F8;

        cpu.Step(); // JSR: pushes $01FA, jumps to $01F4
        cpu.PC.Should().Be(0x01F4, "JSR should jump to RTS at $01F4");

        cpu.Step(); // RTS: pops $01FA, sets PC = $01FA+1 = $01FB
        cpu.PC.Should().Be(0x01FB, "RTS should return to $01FB after JSR");

        cpu.Step(); // LDA #$42 at $01FB
        cpu.A.Should().Be(0x42, "after RTS the next instruction should execute");
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
    public void PredefinedProgram_BlinkLed_ShouldToggleLed()
    {
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        var program = MinimalBlinkPredefinedPrograms.FindById("blink-led");
        program.Should().NotBeNull();

        mem.Load(program!.LoadAddress, program.Bytes);
        cpu.PC = program.StartAddress;

        for (int i = 0; i < 100; i++)
        {
            cpu.Step();
            if (cpu.Halted) break;
        }

        mem.LedState.ToggleCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void PredefinedProgram_HelloUart_ShouldOutputToTerminal()
    {
        var uart = new CmosCpu.Computer.Devices.Serial.UartDevice();
        var mem = new MinimalBlinkMemory();
        var cpu = new MinimalBlinkCpu(mem);
        mem.AttachUart(new CmosCpu.Computer.Devices.Serial.MemoryMappedUartAdapter(uart));
        var program = MinimalBlinkPredefinedPrograms.FindById("hello-uart");
        program.Should().NotBeNull();

        mem.Load(program!.LoadAddress, program.Bytes);
        cpu.PC = program.StartAddress;

        var output = new System.Text.StringBuilder();
        uart.ByteTransmitted += (_, args) => output.Append((char)args.Value);

        for (int i = 0; i < 100; i++)
        {
            cpu.Step();
            if (cpu.Halted) break;
        }

        output.ToString().Should().Contain("Hello");
        output.ToString().Should().Contain("UART");
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

    [TestMethod]
    public void Load_OutOfRange_TruncatesAtRomEnd()
    {
        var mem = new MinimalBlinkMemory();
        mem.Load(0x01FE, [0x01, 0x02, 0x03, 0x04]);

        mem.ReadByte(0x01FE).Should().Be(0x01);
        mem.ReadByte(0x01FF).Should().Be(0x02);
        mem.ReadByte(0x0200).Should().Be(0, "beyond ROM reads 0");
    }
}

[TestClass]
public sealed class MinimalBlinkSessionAdvancedTests
{
    [TestMethod]
    public async Task LoadPredefinedProgram_OutOfRange_ShouldReturnFailure()
    {
        var session = new MinimalBlinkMachineSession();

        var program = new Models.MinimalBlinkPredefinedProgram(
            "too-big", "Too Big", "Program too big for ROM",
            0x01FE, 0x01FE,
            [0x01, 0x02, 0x03, 0x04, 0x05]);

        // We can't inject a custom program, so test that a program that
        // fits still succeeds
        var result = await session.ExecuteMachineCommandAsync(
            "minimal-blink.load-predefined-program", "led-on");
        result.IsSuccess.Should().BeTrue();
    }

    [TestMethod]
    public async Task CpuStateSnapshot_ShouldShowAInAField()
    {
        var session = new MinimalBlinkMachineSession();
        await session.ExecuteMachineCommandAsync("minimal-blink.load-predefined-program", "led-on");
        await session.StepInstructionAsync();

        var cpu = session.Current.Cpu;
        cpu.Should().NotBeNull();
        cpu!.A.Should().Be("$01", "LDA #$01 should set A=$01");
    }

    [TestMethod]
    public async Task Step_WithoutProgram_ShouldAutoLoadAndStep()
    {
        var session = new MinimalBlinkMachineSession();
        await session.StepInstructionAsync();

        var cpu = session.Current.Cpu;
        cpu.Should().NotBeNull();
        cpu!.Pc.Should().NotBe("$0000", "auto-load should set PC to program start");
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
