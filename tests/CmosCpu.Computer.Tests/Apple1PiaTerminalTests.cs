using CmosCpu.Computer;
using CmosCpu.Core;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class Apple1PiaTerminalTests
{
    internal const string Apple1Profile = """
    {
        "id": "apple-1",
        "name": "Apple-1",
        "cpu": "mos6502",
        "clockHz": 1023000,
        "memory": {
            "ram": [{ "start": "0x0000", "size": "0x2000" }],
            "rom": [{ "start": "0xFF00", "size": "0x0100" }],
            "vectors": { "reset": "0xFF00", "nmi": "0xFF00", "irq": "0xFF00" }
        },
        "devices": [
            { "type": "apple1-pia-terminal", "id": "terminal", "start": "0xD010", "size": "0x0004", "columns": 40, "rows": 24 }
        ]
    }
    """;

    [TestMethod]
    public void KeyboardStatus_ReturnsReady_WhenKeyQueued()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.QueueKey('A');

        terminal.Read(0xD011).Should().Be(0x80);
    }

    [TestMethod]
    public void KeyboardRead_ReturnsKey_AndClearsReady()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.QueueKey('A');

        terminal.Read(0xD010).Should().Be((byte)'A');
        terminal.Read(0xD011).Should().Be(0x00);
    }

    [TestMethod]
    public void DisplayWrite_AppendsCharacter()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.Write(0xD012, (byte)'H');
        terminal.Write(0xD012, (byte)'i');

        terminal.Text.Should().Be("Hi");
    }

    [TestMethod]
    public void DisplayWrite_CarriageReturn_StartsNewLine()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.Write(0xD012, (byte)'A');
        terminal.Write(0xD012, (byte)'\r');
        terminal.Write(0xD012, (byte)'B');

        terminal.Lines.Should().HaveCount(2);
        terminal.Lines[0].Should().Be("A");
        terminal.Lines[1].Should().Be("B");
    }

    [TestMethod]
    public void DisplayStatus_IsReady()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.Read(0xD013).Should().Be(0x80);
    }

    [TestMethod]
    public void DeviceHandles_OnlyApple1PiaAddresses()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.Handles(0xD010).Should().BeTrue();
        terminal.Handles(0xD013).Should().BeTrue();
        terminal.Handles(0xD00F).Should().BeFalse();
        terminal.Handles(0xD014).Should().BeFalse();
    }

    [TestMethod]
    public void Version_Increments_WhenDisplayChanges()
    {
        var terminal = new Apple1PiaTerminalDevice();

        long v0 = terminal.Version;
        terminal.Write(0xD012, (byte)'A');
        terminal.Version.Should().BeGreaterThan(v0);

        long v1 = terminal.Version;
        terminal.Write(0xD012, (byte)'\r');
        terminal.Version.Should().BeGreaterThan(v1);
    }

    [TestMethod]
    public void Apple1Profile_LoadsWithoutException()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Apple1Profile);

        machine.Should().NotBeNull();
        machine.Profile.Id.Should().Be("apple-1");
        machine.Apple1Terminal.Should().NotBeNull();
    }

    [TestMethod]
    public void KeyboardRead_ReturnsLastKey_OnEmptyBuffer()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.QueueKey('X');
        terminal.Read(0xD010);
        byte secondRead = terminal.Read(0xD010);

        secondRead.Should().Be((byte)'X');
    }

    [TestMethod]
    public void DisplayWrite_MasksTo7Bit()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.Write(0xD012, 0x80 | (byte)'A');
        terminal.Write(0xD012, 0x80 | (byte)'B');

        terminal.Text.Should().Be("AB");
    }
}
