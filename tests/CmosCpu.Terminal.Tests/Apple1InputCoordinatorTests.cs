using CmosCpu.Terminal.Tui;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class Apple1InputCoordinatorTests
{
    [TestMethod]
    public void InitialState_IsUnknown()
    {
        var c = new Apple1InputCoordinator();
        c.Mode.Should().Be(Apple1TerminalMode.Unknown);
        c.HasPendingUserLine.Should().BeFalse();
        c.WaitingForPrompt.Should().BeFalse();
    }

    [TestMethod]
    public void OnOutput_WozPrompt_ChangesToWozMonitor()
    {
        var c = new Apple1InputCoordinator();
        c.OnOutput("\n\\");
        c.Mode.Should().Be(Apple1TerminalMode.WozMonitor);
    }

    [TestMethod]
    public void OnOutput_BasicPrompt_ChangesToBasic()
    {
        var c = new Apple1InputCoordinator();
        c.OnOutput("\n>");
        c.Mode.Should().Be(Apple1TerminalMode.Basic);
    }

    [TestMethod]
    public void Reset_ClearsState()
    {
        var c = new Apple1InputCoordinator();
        c.OnOutput("\n>");
        c.EnqueueUserLine("PRINT 1");
        c.Reset();

        c.Mode.Should().Be(Apple1TerminalMode.Unknown);
        c.WaitingForPrompt.Should().BeFalse();
        c.HasPendingUserLine.Should().BeFalse();
    }

    [TestMethod]
    public void EnqueueUserLine_InBasicMode_QueuesLineAndAwaitsPrompt()
    {
        var c = new Apple1InputCoordinator();
        c.OnOutput("\n>");

        c.EnqueueUserLine("PRINT 1");

        c.WaitingForPrompt.Should().BeTrue();
        c.UserQueueCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void EnqueueUserLine_NotInBasic_BuffersPendingLine()
    {
        var c = new Apple1InputCoordinator();
        c.OnOutput("\n\\");

        c.EnqueueUserLine("PRINT 1");

        c.HasPendingUserLine.Should().BeTrue();
        c.UserQueueCount.Should().Be(0);
    }

    [TestMethod]
    public void PendingLine_IsReleased_WhenBasicDetected()
    {
        var c = new Apple1InputCoordinator();
        c.EnqueueUserLine("PRINT 1");

        c.OnOutput("\n>");

        c.HasPendingUserLine.Should().BeFalse();
        c.UserQueueCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void PendingLine_ContainsFullLineWithCR()
    {
        var c = new Apple1InputCoordinator();
        c.EnqueueUserLine("HELLO");
        c.OnOutput("\n>");

        var chars = new List<char>();
        while (c.TryDequeueNextChar(out char ch))
            chars.Add(ch);

        string s = string.Concat(chars);
        s.Should().Be("HELLO\r");
    }

    [TestMethod]
    public void TryDequeueNextChar_Empty_ReturnsFalse()
    {
        var c = new Apple1InputCoordinator();
        c.TryDequeueNextChar(out _).Should().BeFalse();
    }

    [TestMethod]
    public void TraceState_WhenTrue_WritesToTraceWriter()
    {
        var c = new Apple1InputCoordinator { TraceState = true };
        var writer = new StringWriter();
        c.TraceWriter = writer;

        c.OnOutput("\n\\");

        string trace = writer.ToString();
        trace.Should().Contain("State:");
    }

    [TestMethod]
    public void TraceState_WhenFalse_DoesNotWrite()
    {
        var c = new Apple1InputCoordinator { TraceState = false };
        var writer = new StringWriter();
        c.TraceWriter = writer;

        c.OnOutput("\n\\");

        writer.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public void WozMonitor_DoesNotEnqueueAnyInput()
    {
        var c = new Apple1InputCoordinator();
        c.OnOutput("\n\\");
        c.TryDequeueNextChar(out _).Should().BeFalse();
    }
}
