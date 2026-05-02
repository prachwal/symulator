using CmosCpu.Computer;
using CmosCpu.Terminal.Session;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class ComputerSessionTests
{
    private const string TestProfile = """
    {
        "id": "test",
        "name": "Test",
        "cpu": "mos6502",
        "clockHz": 1000000,
        "memory": {
            "ram": [{ "start": "0x0000", "size": "0x8000" }],
            "rom": [{ "start": "0xF000", "size": "0x1000" }],
            "vectors": { "reset": "0xF000", "nmi": "0xF100", "irq": "0xF200" }
        },
        "devices": [
            { "type": "text-display", "id": "screen", "start": "0xD000", "width": 40, "height": 25 },
            { "type": "keyboard", "id": "keyboard", "dataAddress": "0xD800", "statusAddress": "0xD801" }
        ]
    }
    """;

    [TestMethod]
    public void LoadProfile_ShouldSetMachine()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        session.IsLoaded.Should().BeTrue();
        session.Machine.Should().NotBeNull();
        session.Machine!.Profile.Name.Should().Be("Test");
    }

    [TestMethod]
    public void LoadProfile_WithNullJson_ShouldNotLoad()
    {
        var session = new ComputerSession();
        session.LoadProfile("invalid json").Should().BeFalse();
        session.IsLoaded.Should().BeFalse();
    }

    [TestMethod]
    public void Reset_ShouldResetCpu()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        session.Step();
        ushort pcAfterStep = session.Machine!.Cpu.PC;

        session.Reset();
        // After reset, CPU reads reset vector from 0xFFFC-0xFFFD
        // Without ROM, these are 0x0000, so PC = 0x0000
        // Reset adds 7 cycles
        session.Machine.Cpu.PC.Should().Be(0x0000);
    }

    [TestMethod]
    public void Step_ShouldExecuteInstruction()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        ulong before = session.Machine!.Cpu.CycleCount;
        int cycles = session.Step();

        cycles.Should().BeGreaterThan(0);
        session.Machine.Cpu.CycleCount.Should().Be(before + (ulong)cycles);
    }

    [TestMethod]
    public void Step_NoMachine_ReturnsZero()
    {
        var session = new ComputerSession();
        session.Step().Should().Be(0);
    }

    [TestMethod]
    public void ReadMemory_ShouldReturnData()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        byte[] data = session.ReadMemory(0x0000, 256);

        data.Length.Should().Be(256);
    }

    [TestMethod]
    public void ReadMemory_NoMachine_ReturnsEmpty()
    {
        var session = new ComputerSession();
        session.ReadMemory(0x0000, 256).Should().BeEmpty();
    }

    [TestMethod]
    public void WriteMemory_ShouldWriteData()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        session.WriteMemory(0x1000, [0x42, 0x43]);
        byte[] data = session.ReadMemory(0x1000, 2);

        data[0].Should().Be(0x42);
        data[1].Should().Be(0x43);
    }

    [TestMethod]
    public void LoadBinary_ShouldWriteAtAddress()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        session.LoadBinary([0xAA, 0xBB], 0x2000);
        byte[] data = session.ReadMemory(0x2000, 2);

        data[0].Should().Be(0xAA);
        data[1].Should().Be(0xBB);
    }

    [TestMethod]
    public void SetSpeed_ShouldUpdateValues()
    {
        var session = new ComputerSession();
        session.SetSpeed(500, 50);
        var (batch, delay) = session.GetSpeed();

        batch.Should().Be(500);
        delay.Should().Be(50);
    }

    [TestMethod]
    public void GetSpeed_DefaultValues()
    {
        var session = new ComputerSession();
        var (batch, delay) = session.GetSpeed();

        batch.Should().Be(1000);
        delay.Should().Be(16);
    }

    [TestMethod]
    public void IsRunning_InitiallyFalse()
    {
        var session = new ComputerSession();
        session.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void Stop_NoRun_DoesNotThrow()
    {
        var session = new ComputerSession();
        session.Stop();
    }

    [TestMethod]
    public void LoadBinary_NoMachine_ReturnsFalse()
    {
        var session = new ComputerSession();
        session.LoadBinary([0x01], 0x0000).Should().BeFalse();
    }

    [TestMethod]
    public void LoadBinaryToRom_ShouldLoadRom()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        byte[] rom = new byte[0x1000];
        rom[0] = 0xA9; rom[1] = 0x42; // LDA #$42
        rom[0xFFC] = 0x00; rom[0xFFD] = 0xF0;

        session.LoadBinaryToRom(rom, 0xF000).Should().BeTrue();
    }

    [TestMethod]
    public void CompileAndLoad_ShouldCompileAsm()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        string source = """
        .org 0x8000
            LDA #0x42
            HLT
        """;

        session.CompileAndLoad(source).Should().BeTrue();
    }

    [TestMethod]
    public void TextDisplay_AccessibleThroughMachine()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        session.Machine!.TextDisplay.Should().NotBeNull();
        session.Machine.TextDisplay!.Width.Should().Be(40);
        session.Machine.TextDisplay.Height.Should().Be(25);
    }

    [TestMethod]
    public void Keyboard_AccessibleThroughMachine()
    {
        var session = new ComputerSession();
        session.LoadProfile(TestProfile);

        session.Machine!.Keyboard.Should().NotBeNull();
    }
}
