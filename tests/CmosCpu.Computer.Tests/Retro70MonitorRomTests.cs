using CmosCpu.Computer;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class Retro70MonitorRomTests
{
    [TestMethod]
    public void Build_ReturnsCorrectSize()
    {
        var rom = Retro70MonitorRomBuilder.Build(0x1000);
        rom.Length.Should().Be(0x1000);
    }

    [TestMethod]
    public void Build_ResetVectorPointsTo0xF000()
    {
        var rom = Retro70MonitorRomBuilder.Build();

        rom[0xFFC].Should().Be(0x00);
        rom[0xFFD].Should().Be(0xF0);
    }

    [TestMethod]
    public void Build_NmiVectorPointsTo0xF100()
    {
        var rom = Retro70MonitorRomBuilder.Build();

        rom[0xFFA].Should().Be(0x00);
        rom[0xFFB].Should().Be(0xF1);
    }

    [TestMethod]
    public void Build_IrqVectorPointsTo0xF200()
    {
        var rom = Retro70MonitorRomBuilder.Build();

        rom[0xFFE].Should().Be(0x00);
        rom[0xFFF].Should().Be(0xF2);
    }

    [TestMethod]
    public void AfterReset_PcEquals0xF000()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(ComputerMachineTests.Retro70Profile);
        var rom = Retro70MonitorRomBuilder.Build();
        machine.Memory.LoadRom(0xF000, rom);

        machine.Reset();

        machine.Cpu.PC.Should().Be(0xF000);
    }

    [TestMethod]
    public void AfterExecution_ScreenContainsRetro70Ready()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(ComputerMachineTests.Retro70Profile);
        var rom = Retro70MonitorRomBuilder.Build();
        machine.Memory.LoadRom(0xF000, rom);

        machine.Reset();

        // Run enough steps to print "RETRO70 READY" + ">" = 15 chars
        // Each char: LDA (4) + BEQ (2/3) + STA (5) + INX (2) + JMP (3) = ~16 cycles per char
        // 15 chars * 16 = 240 cycles + overhead. Use more to be safe.
        machine.RunSteps(500);

        var display = machine.TextDisplay!;
        var buffer = display.GetBuffer();

        // Check first 13 characters = "RETRO70 READY"
        string expected = "RETRO70 READY";
        for (int i = 0; i < expected.Length; i++)
            buffer[i].Should().Be((byte)expected[i], $"char at position {i} should be '{expected[i]}'");

        // Check character after CR is '>'
        buffer[14].Should().Be((byte)'>');
    }
}
