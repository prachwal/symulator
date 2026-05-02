using CmosCpu.Computer;
using CmosCpu.Terminal.Session;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class ProfileIntegrationTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));

    private string ProfilePath(string name) => Path.Combine(RepoRoot, "profiles", name);

    [TestMethod]
    public void Retro70_Profile_ShouldBootToF000()
    {
        var session = new ComputerSession();
        session.LoadProfileFromFile(ProfilePath("retro70-mos6502.json")).Should().BeTrue();
        session.Machine!.Cpu.PC.Should().Be(0xF000);
        session.Machine.Cpu.CycleCount.Should().Be(7);
    }

    [TestMethod]
    public void Apple1_Profile_ShouldBootToFf00()
    {
        var session = new ComputerSession();
        session.LoadProfileFromFile(ProfilePath("apple-1.json")).Should().BeTrue();
        session.Machine!.Cpu.PC.Should().Be(0xFF00);
    }

    [TestMethod]
    public void Kim1_Profile_ShouldNotBootToFfffOpenBus()
    {
        var session = new ComputerSession();
        session.LoadProfileFromFile(ProfilePath("kim-1.json")).Should().BeTrue();
        session.Machine!.Cpu.PC.Should().NotBe(0xFFFF);
        session.Machine.Cpu.PC.Should().Be(0x1C22);
    }

    [TestMethod]
    public void Kim1_Profile_ResetVector_ShouldComeFromMirroredRom()
    {
        var session = new ComputerSession();
        session.LoadProfileFromFile(ProfilePath("kim-1.json"));

        ushort vector = (ushort)(session.Machine!.Memory.ReadByte(0xFFFC)
                       | (session.Machine.Memory.ReadByte(0xFFFD) << 8));
        vector.Should().Be(0x1C22);
    }

    [TestMethod]
    public void Kim1_Profile_FirstStep_ShouldNotFailWithOpcodeFF()
    {
        var session = new ComputerSession();
        session.LoadProfileFromFile(ProfilePath("kim-1.json"));

        int cycles = session.Step();
        cycles.Should().BeGreaterThan(0);
        session.Machine!.Cpu.IsHalted.Should().BeFalse();
    }

    [TestMethod]
    public void Apple1_Profile_WozMonitor_ShouldStepSuccessfully()
    {
        var session = new ComputerSession();
        session.LoadProfileFromFile(ProfilePath("apple-1.json"));

        for (int i = 0; i < 10; i++)
        {
            int cycles = session.Step();
            cycles.Should().BeGreaterThan(0);
            session.Machine!.Cpu.IsHalted.Should().BeFalse();
        }
    }

    [TestMethod]
    public void Retro70_Profile_ShouldStepSuccessfully()
    {
        var session = new ComputerSession();
        session.LoadProfileFromFile(ProfilePath("retro70-mos6502.json"));

        for (int i = 0; i < 10; i++)
        {
            int cycles = session.Step();
            cycles.Should().BeGreaterThan(0);
            session.Machine!.Cpu.IsHalted.Should().BeFalse();
        }
    }

    [TestMethod]
    public void RomLoader_NotFound_DoesNotCrash()
    {
        var session = new ComputerSession();
        const string profileWithBadRom = """
        {
            "id": "test-bad-rom",
            "name": "Test Bad ROM",
            "cpu": "mos6502",
            "clockHz": 1000000,
            "memory": {
                "ram": [{ "start": "0x0000", "size": "0x8000" }],
                "rom": [{ "start": "0xF000", "size": "0x1000", "file": "nonexistent-rom.bin" }]
            }
        }
        """;

        session.LoadProfile(profileWithBadRom).Should().BeTrue();
        session.IsLoaded.Should().BeTrue();
    }

    [TestMethod]
    public void Profile_FromFile_LoadsRomsSuccessfully()
    {
        var session = new ComputerSession();
        session.LoadProfileFromFile(ProfilePath("retro70-mos6502.json")).Should().BeTrue();

        byte[] romData = session.ReadMemory(0xF000, 16);
        romData.Should().NotContain(b => b == 0xFF); // ROM loaded, not open bus
    }

    [TestMethod]
    public void Apple1Pia_KeyboardReady_ShouldBeSignaledAfterKeyQueued()
    {
        var pia = new Apple1PiaTerminalDevice(40, 24);
        pia.Read(0xD011).Should().Be(0);

        pia.QueueKey('A');
        pia.Read(0xD011).Should().Be(0x80);
    }

    [TestMethod]
    public void Apple1Pia_ReadKeyboardData_ShouldReturnQueuedCharacter()
    {
        var pia = new Apple1PiaTerminalDevice(40, 24);
        pia.QueueKey('A');

        byte data = pia.Read(0xD010);
        data.Should().Be(unchecked((byte)'A' | 0x80));
    }

    [TestMethod]
    public void Apple1Pia_ReadKeyboardData_ShouldClearReadyFlag()
    {
        var pia = new Apple1PiaTerminalDevice(40, 24);
        pia.QueueKey('A');
        pia.Read(0xD010);
        pia.Read(0xD011).Should().Be(0);
    }

    [TestMethod]
    public void Apple1Pia_WriteDisplayData_ShouldAppendToTerminalText()
    {
        var pia = new Apple1PiaTerminalDevice(40, 24);
        pia.Write(0xD012, (byte)'H');
        pia.Write(0xD012, (byte)'i');

        pia.Text.Should().Contain("Hi");
    }

    [TestMethod]
    public void Apple1Pia_DisplayStatus_ShouldBeReady()
    {
        var pia = new Apple1PiaTerminalDevice(40, 24);
        pia.Read(0xD013).Should().Be(0x80);
    }

    [TestMethod]
    public void Apple1Pia_Newline_ShouldFlushCurrentLine()
    {
        var pia = new Apple1PiaTerminalDevice(40, 24);
        pia.Write(0xD012, (byte)'A');
        pia.Write(0xD012, (byte)'\r');

        pia.Lines.Should().Contain("A");
    }
}
