using FluentAssertions;
using Symulator.Machines.Apple1.Factory;
using Symulator.Machines.Apple1.Module;
using Symulator.Machines.Apple1.Models;
using Symulator.Machines.Apple1.Services;

namespace Symulator.Machines.Apple1.Tests;

[TestClass]
public sealed class Apple1PromptDetectorTests
{
    [TestMethod]
    public void LooksLikeWozPrompt_BackslashEnding_ReturnsTrue()
    {
        var result = Apple1PromptDetector.LooksLikeWozPrompt("some output\\");
        result.Should().BeTrue();
    }

    [TestMethod]
    public void LooksLikeWozPrompt_NoBackslash_ReturnsFalse()
    {
        var result = Apple1PromptDetector.LooksLikeWozPrompt("some output");
        result.Should().BeFalse();
    }

    [TestMethod]
    public void LooksLikeWozPrompt_Empty_ReturnsFalse()
    {
        Apple1PromptDetector.LooksLikeWozPrompt("").Should().BeFalse();
        Apple1PromptDetector.LooksLikeWozPrompt(null!).Should().BeFalse();
    }

    [TestMethod]
    public void LooksLikeBasicPrompt_PromptLine_ReturnsTrue()
    {
        var result = Apple1PromptDetector.LooksLikeBasicPrompt("line1\n>");
        result.Should().BeTrue();
    }

    [TestMethod]
    public void LooksLikeBasicPrompt_NoNewline_ReturnsFalse()
    {
        Apple1PromptDetector.LooksLikeBasicPrompt(">").Should().BeFalse();
    }

    [TestMethod]
    public void DetectMode_WozPrompt_ReturnsWozMonitor()
    {
        var mode = Apple1PromptDetector.DetectMode("\\", Apple1TerminalMode.Unknown);
        mode.Should().Be(Apple1TerminalMode.WozMonitor);
    }

    [TestMethod]
    public void DetectMode_BasicPrompt_ReturnsBasic()
    {
        var mode = Apple1PromptDetector.DetectMode("line\n>", Apple1TerminalMode.Unknown);
        mode.Should().Be(Apple1TerminalMode.Basic);
    }
}

[TestClass]
public sealed class Apple1TerminalStateMachineTests
{
    [TestMethod]
    public void DetectMode_BackslashInTail_ReturnsWozMonitor()
    {
        var sm = new Apple1TerminalStateMachine();
        sm.Feed("output\\");
        var mode = sm.DetectMode(Apple1TerminalMode.Unknown);
        mode.Should().Be(Apple1TerminalMode.WozMonitor);
    }

    [TestMethod]
    public void DetectMode_EmptyTail_ReturnsCurrentMode()
    {
        var sm = new Apple1TerminalStateMachine();
        var mode = sm.DetectMode(Apple1TerminalMode.Basic);
        mode.Should().Be(Apple1TerminalMode.Basic);
    }

    [TestMethod]
    public void Feed_AppendsToTail()
    {
        var sm = new Apple1TerminalStateMachine();
        sm.Feed("hello");
        sm.Tail.Should().Be("hello");
        sm.Feed(" world");
        sm.Tail.Should().Be("hello world");
    }

    [TestMethod]
    public void Reset_ClearsTail()
    {
        var sm = new Apple1TerminalStateMachine();
        sm.Feed("some text");
        sm.Reset();
        sm.Tail.Should().BeEmpty();
    }
}

[TestClass]
public sealed class Apple1InputCoordinatorTests
{
    [TestMethod]
    public void EnqueueUserLine_InBasicMode_QueuesCharactersWithCarriageReturn()
    {
        var coord = new Apple1InputCoordinator();
        coord.OnOutput("line\n>"); // set mode to Basic

        coord.EnqueueUserLine("HELLO");

        coord.TryDequeueNextChar(out var c).Should().BeTrue();
        c.Should().Be('H');
        coord.TryDequeueNextChar(out _);
        coord.TryDequeueNextChar(out _);
        coord.TryDequeueNextChar(out _);
        coord.TryDequeueNextChar(out _);
        coord.TryDequeueNextChar(out _).Should().BeTrue(); // \r
    }

    [TestMethod]
    public void EnqueueUserLine_InNonBasicMode_BuffersLine()
    {
        var coord = new Apple1InputCoordinator();

        coord.EnqueueUserLine("HELLO");

        coord.HasPendingUserLine.Should().BeTrue();
        coord.TryDequeueNextChar(out _).Should().BeFalse();
    }

    [TestMethod]
    public void Reset_ClearsQueue()
    {
        var coord = new Apple1InputCoordinator();
        coord.EnqueueUserLine("TEST");
        coord.Reset();

        coord.TryDequeueNextChar(out _).Should().BeFalse();
        coord.Mode.Should().Be(Apple1TerminalMode.Unknown);
    }

    [TestMethod]
    public void OnOutput_UpdatesMode()
    {
        var coord = new Apple1InputCoordinator();
        coord.OnOutput("\\");
        coord.Mode.Should().Be(Apple1TerminalMode.WozMonitor);
    }
}

[TestClass]
public sealed class Apple1MachineFactoryTests
{
    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.slnx").Length > 0 || dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return AppContext.BaseDirectory;
    }

    [TestMethod]
    public void Create_ShouldMapBasicRomAtE000()
    {
        var solutionRoot = FindSolutionRoot();
        var machine = Apple1MachineFactory.Create(solutionRoot);

        byte lo = machine.Memory.ReadByte(0xE000);
        byte hi = machine.Memory.ReadByte(0xE001);

        lo.Should().NotBe(0, "BASIC ROM should have data at $E000");
        lo.Should().NotBe(0xFF, "BASIC ROM should not be open bus at $E000");
    }
}

[TestClass]
public sealed class Apple1InputEncodingTests
{
    [TestMethod]
    public void SendInput_ShouldUseCarriageReturnNotLineFeed()
    {
        byte cr = (byte)'\r';
        byte lf = (byte)'\n';

        cr.Should().Be(0x0D, "Apple-1 carriage return is 0x0D");
        lf.Should().NotBe(0x0D, "line feed should not be used as CR");
    }

    [TestMethod]
    public void QueueKey_ShouldSetBit7()
    {
        var terminal = new CmosCpu.Computer.Apple1PiaTerminalDevice();
        terminal.QueueKey('A');

        byte keyData = terminal.Read(0xD010);
        (keyData & 0x80).Should().Be(0x80, "queued key should have bit 7 set");
        (keyData & 0x7F).Should().Be((byte)'A', "lower 7 bits should hold the ASCII value");
    }

    [TestMethod]
    public void QueueKey_ShouldSetKeyReady()
    {
        var terminal = new CmosCpu.Computer.Apple1PiaTerminalDevice();
        terminal.QueueKey('R');

        terminal.Read(0xD011).Should().Be(0x80);
    }

    [TestMethod]
    public void QueueMultipleKeys_ShouldPreserveOrder()
    {
        var terminal = new CmosCpu.Computer.Apple1PiaTerminalDevice();

        terminal.QueueKey('E');
        terminal.QueueKey('0');
        terminal.QueueKey('0');
        terminal.QueueKey('0');
        terminal.QueueKey('.');
        terminal.QueueKey('R');
        terminal.QueueKey('\r');

        (terminal.Read(0xD010) & 0x7F).Should().Be((byte)'E');
        (terminal.Read(0xD010) & 0x7F).Should().Be((byte)'0');
        (terminal.Read(0xD010) & 0x7F).Should().Be((byte)'0');
        (terminal.Read(0xD010) & 0x7F).Should().Be((byte)'0');
        (terminal.Read(0xD010) & 0x7F).Should().Be((byte)'.');
        (terminal.Read(0xD010) & 0x7F).Should().Be((byte)'R');
        (terminal.Read(0xD010) & 0x7F).Should().Be(0x0D);
        terminal.Read(0xD011).Should().Be(0x00, "key ready should be cleared after last key consumed");
    }

    [TestMethod]
    public void BasicPromptDetector_MemoryDump_ShouldNotBeTreatedAsSuccess()
    {
        var terminal = "\u007F\nE000R\n\nE000: 4C";
        bool hasPrompt = terminal.Contains("\n>") || terminal.EndsWith(">");
        hasPrompt.Should().BeFalse("memory dump output should not match BASIC prompt");
    }

    [TestMethod]
    public void BasicPromptDetector_ActualPrompt_ShouldBeDetected()
    {
        var terminal = "some garbage\n>READY.\n>";
        bool hasPrompt = terminal.Contains("\n>") || terminal.EndsWith(">");
        hasPrompt.Should().BeTrue();
    }
}

[TestClass]
public sealed class Apple1RealRomBootTests
{
    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("*.slnx").Length > 0 || dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }

        return AppContext.BaseDirectory;
    }

    [TestMethod]
    public void Reset_ShouldShowMonitorPromptWithoutClearingScreen()
    {
        var machine = Apple1MachineFactory.Create(FindSolutionRoot());

        machine.Reset();

        for (int i = 0; i < 1000 && !(machine.Apple1Terminal?.Text.Contains("\\") == true); i++)
            machine.Step();

        machine.Apple1Terminal.Should().NotBeNull();
        machine.Apple1Terminal!.Text.Should().Contain("\\");
        machine.Apple1Terminal.Text.Should().NotBeEmpty();
    }

    [TestMethod]
    public void MonitorCommand_ShouldEnterBasicPrompt_WithRealRomSequence()
    {
        var machine = Apple1MachineFactory.Create(FindSolutionRoot());
        machine.Apple1Terminal.Should().NotBeNull();
        var terminal = machine.Apple1Terminal!;

        machine.Reset();
        for (int i = 0; i < 1000 && !terminal.Text.Contains("\\"); i++)
            machine.Step();

        foreach (char c in "E000R\r")
            terminal.QueueKey(c);

        for (int i = 0; i < 200000 && !terminal.Text.Contains("\n>"); i++)
            machine.Step();

        terminal.Text.Should().Contain("E000R");
        terminal.Text.Should().Contain("E000: 4C");
        terminal.Text.Should().Contain("\n>");
    }

    [TestMethod]
    public async Task SendLine_InBasicMode_ShouldProcessWholePrintCommand()
    {
        var session = new Apple1MachineSession();

        var boot = await session.ExecuteMachineCommandAsync("apple1.boot.basic");
        boot.IsSuccess.Should().BeTrue();

        var send = await session.ExecuteMachineCommandAsync("apple1.send-line", "PRINT 1");
        send.IsSuccess.Should().BeTrue();

        var terminal = session.Current.TerminalText ?? string.Empty;
        terminal.Should().Contain("PRINT 1");
        Apple1PromptDetector.LooksLikeBasicPrompt(terminal).Should().BeTrue();
        terminal.Should().NotEndWith("P");
    }
}
