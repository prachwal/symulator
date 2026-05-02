using FluentAssertions;
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
