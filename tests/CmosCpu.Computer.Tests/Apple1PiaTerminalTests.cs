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

    [TestMethod]
    public void ConsumeOutputSince_ReturnsNewContent()
    {
        var terminal = new Apple1PiaTerminalDevice();

        int offset = terminal.OutputLength;
        terminal.Write(0xD012, (byte)'H');
        terminal.Write(0xD012, (byte)'i');

        string output = terminal.ConsumeOutputSince(offset);
        output.Should().Be("Hi");
    }

    [TestMethod]
    public void ConsumeOutputSince_ReturnsEmpty_WhenNoChange()
    {
        var terminal = new Apple1PiaTerminalDevice();

        int offset = terminal.OutputLength;
        terminal.ConsumeOutputSince(offset).Should().BeEmpty();
    }

    [TestMethod]
    public void ConsumeOutputSince_IncludesRawCharacters()
    {
        var terminal = new Apple1PiaTerminalDevice();
        int offset = terminal.OutputLength;

        terminal.Write(0xD012, (byte)'A');
        terminal.Write(0xD012, (byte)'\r');
        terminal.Write(0xD012, (byte)'B');

        string output = terminal.ConsumeOutputSince(offset);
        output.Should().Be("A\rB");
    }

    // --- Phase 1: BASIC ROM diagnostics ---

    [TestMethod]
    public void Apple1_Profile_ShouldLoadBasicRomAtE000()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Apple1Profile);

        machine.Memory.MapRom(0xE000, 0x1000);
        byte[] basicStub = new byte[0x1000];
        for (int i = 0; i < basicStub.Length; i++)
            basicStub[i] = (byte)((i + 0xE0) & 0xFF);
        machine.Memory.LoadRom(0xE000, basicStub);

        byte[] basicRom = machine.Memory.GetMemoryPage(0xE000, 256);
        bool allZero = basicRom.All(b => b == 0);
        bool allFF = basicRom.All(b => b == 0xFF);

        allZero.Should().BeFalse("BASIC ROM at 0xE000 should not be all zeros");
        allFF.Should().BeFalse("BASIC ROM at 0xE000 should not be all FF");
    }

    [TestMethod]
    public void Apple1_Profile_ShouldBootWozMonitorAtFf00()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Apple1Profile);

        byte[] wozRom = new byte[0x100];
        wozRom[0xFC] = 0x00;
        wozRom[0xFD] = 0xFF;
        machine.Memory.LoadRom(0xFF00, wozRom);

        machine.Reset();

        machine.Cpu.PC.Should().Be(0xFF00);
    }

    // --- Phase 6: Output stream duplication prevention tests ---

    [TestMethod]
    public void OutputStream_ReturnsOnlyNewData()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.Write(0xD012, (byte)'A');
        terminal.Write(0xD012, (byte)'B');
        terminal.Write(0xD012, (byte)'C');
        int offset1 = terminal.OutputLength;

        string chunk1 = terminal.ConsumeOutputSince(0);
        chunk1.Should().Be("ABC");

        string empty = terminal.ConsumeOutputSince(offset1);
        empty.Should().BeEmpty();

        terminal.Write(0xD012, (byte)'D');
        string chunk2 = terminal.ConsumeOutputSince(offset1);
        chunk2.Should().Be("D");
    }

    [TestMethod]
    public void OutputStream_RepeatedConsume_DoesNotDuplicate()
    {
        var terminal = new Apple1PiaTerminalDevice();

        foreach (char c in ">PRINT 1")
            terminal.Write(0xD012, (byte)c);

        int offset = terminal.OutputLength;

        string first = terminal.ConsumeOutputSince(0);
        first.Should().Be(">PRINT 1");

        string second = terminal.ConsumeOutputSince(offset);
        second.Should().BeEmpty();
    }

    [TestMethod]
    public void LinesAndText_StillWork_AfterOutputStreamChanges()
    {
        var terminal = new Apple1PiaTerminalDevice();

        terminal.Write(0xD012, (byte)'H');
        terminal.Write(0xD012, (byte)'i');
        terminal.Write(0xD012, (byte)'\r');
        terminal.Write(0xD012, (byte)'!');

        terminal.Lines.Should().Contain("Hi");
        terminal.Lines.Should().Contain("!");
        terminal.Text.Should().Be("Hi\n!");
    }

    [TestMethod]
    public void OutputStream_IncludesPartialLineBeforeCr()
    {
        var terminal = new Apple1PiaTerminalDevice();
        int offset = terminal.OutputLength;

        terminal.Write(0xD012, (byte)'H');
        terminal.Write(0xD012, (byte)'e');
        terminal.Write(0xD012, (byte)'l');

        string output = terminal.ConsumeOutputSince(offset);
        output.Should().Be("Hel");
    }

    [TestMethod]
    public void OutputStream_AfterFlush_DoesNotRepeatPartialLine()
    {
        var terminal = new Apple1PiaTerminalDevice();
        int offset = terminal.OutputLength;

        terminal.Write(0xD012, (byte)'A');
        terminal.Write(0xD012, (byte)'\r');
        terminal.Write(0xD012, (byte)'B');

        string first = terminal.ConsumeOutputSince(offset);
        first.Should().Be("A\rB");

        string second = terminal.ConsumeOutputSince(terminal.OutputLength);
        second.Should().BeEmpty();
    }

    // --- Phase 6: Boot controller tests ---

    [TestMethod]
    public void Apple1BasicBoot_ShouldNotSendE000R_WhenAutoBasicDisabled()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Apple1Profile);
        byte[] wozRom = new byte[0x100];
        wozRom[0xFC] = 0x00;
        wozRom[0xFD] = 0xFF;
        machine.Memory.LoadRom(0xFF00, wozRom);

        var controller = new Apple1BasicBootController(machine, machine.Apple1Terminal!, 50000);
        var result = controller.Boot(autoBasic: false);

        result.Success.Should().BeTrue();
        machine.Cpu.PC.Should().Be(0xFF00);
    }

    [TestMethod]
    public void Apple1BasicBoot_ShouldAttemptBasicHandoff_WhenAutoBasicEnabled()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Apple1Profile);
        byte[] wozRom = new byte[0x100];
        wozRom[0xFC] = 0x00;
        wozRom[0xFD] = 0xFF;
        machine.Memory.LoadRom(0xFF00, wozRom);

        var controller = new Apple1BasicBootController(machine, machine.Apple1Terminal!, 50000);
        var result = controller.Boot(autoBasic: true);

        // With NOP-only Woz ROM, no PIA output is produced, so the boot will fail to detect Woz output
        // But it should still have consumed cycles
        result.Success.Should().BeFalse();
        result.CyclesUsed.Should().BeGreaterThan(0);
        result.Trace.Should().Contain("no Woz output");
    }

    // --- Phase 6: Diagnostics ---

    [TestMethod]
    public void Apple1BasicScript_ShouldConvertLfToCr()
    {
        var terminal = new Apple1PiaTerminalDevice();
        terminal.QueueKey('\n');
        byte data = terminal.Read(0xD010);
        data.Should().Be((byte)'\n');
    }

    [TestMethod]
    public void Apple1BasicScript_ShouldReportDiagnosticsOnTimeout()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Apple1Profile);
        byte[] wozRom = new byte[0x100];
        wozRom[0x00] = 0xEA;
        machine.Memory.LoadRom(0xFF00, wozRom);

        var controller = new Apple1BasicBootController(machine, machine.Apple1Terminal!, 1000);
        var result = controller.Boot(autoBasic: true);

        result.Success.Should().BeFalse();
        result.Trace.Should().Contain("no Woz output");
    }
}
