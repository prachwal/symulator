using CmosCpu.Terminal.Tui.Apple1;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class Apple1PromptDetectorTests
{
    [TestMethod]
    public void LooksLikeWozPrompt_NullOrEmpty_ReturnsFalse()
    {
        Apple1PromptDetector.LooksLikeWozPrompt(null!).Should().BeFalse();
        Apple1PromptDetector.LooksLikeWozPrompt("").Should().BeFalse();
        Apple1PromptDetector.LooksLikeWozPrompt("   ").Should().BeFalse();
    }

    [TestMethod]
    public void LooksLikeWozPrompt_BackslashAtEnd_ReturnsTrue()
    {
        Apple1PromptDetector.LooksLikeWozPrompt("\n\\").Should().BeTrue();
        Apple1PromptDetector.LooksLikeWozPrompt("0010: 00\n\\").Should().BeTrue();
        Apple1PromptDetector.LooksLikeWozPrompt("\\").Should().BeTrue();
    }

    [TestMethod]
    public void LooksLikeWozPrompt_NoBackslash_ReturnsFalse()
    {
        Apple1PromptDetector.LooksLikeWozPrompt(">PRINT 1").Should().BeFalse();
        Apple1PromptDetector.LooksLikeWozPrompt("hello world").Should().BeFalse();
        Apple1PromptDetector.LooksLikeWozPrompt("\\ not at end").Should().BeFalse();
    }

    [TestMethod]
    public void LooksLikeBasicPrompt_NullOrEmpty_ReturnsFalse()
    {
        Apple1PromptDetector.LooksLikeBasicPrompt(null!).Should().BeFalse();
        Apple1PromptDetector.LooksLikeBasicPrompt("").Should().BeFalse();
        Apple1PromptDetector.LooksLikeBasicPrompt("   ").Should().BeFalse();
    }

    [TestMethod]
    public void LooksLikeBasicPrompt_GreaterThanAtLineEnd_ReturnsTrue()
    {
        Apple1PromptDetector.LooksLikeBasicPrompt("\n>").Should().BeTrue();
        Apple1PromptDetector.LooksLikeBasicPrompt(">PRINT 1\n1\n>").Should().BeTrue();
    }

    [TestMethod]
    public void LooksLikeBasicPrompt_GreaterThanInMiddle_ReturnsFalse()
    {
        Apple1PromptDetector.LooksLikeBasicPrompt("A > B").Should().BeFalse();
        Apple1PromptDetector.LooksLikeBasicPrompt(">PRINT 1").Should().BeFalse();
        Apple1PromptDetector.LooksLikeBasicPrompt(">>>>>").Should().BeFalse();
    }

    [TestMethod]
    public void DetectMode_BasicPrompt_ReturnsBasic()
    {
        Apple1PromptDetector.DetectMode("\n>", Apple1TerminalMode.Unknown)
            .Should().Be(Apple1TerminalMode.Basic);
        Apple1PromptDetector.DetectMode(">PRINT 1\n1\n>", Apple1TerminalMode.Basic)
            .Should().Be(Apple1TerminalMode.Basic);
    }

    [TestMethod]
    public void DetectMode_WozPrompt_ReturnsWozMonitor()
    {
        Apple1PromptDetector.DetectMode("\n\\", Apple1TerminalMode.Unknown)
            .Should().Be(Apple1TerminalMode.WozMonitor);
        Apple1PromptDetector.DetectMode("0010: 00\n\\", Apple1TerminalMode.Booting)
            .Should().Be(Apple1TerminalMode.WozMonitor);
    }

    [TestMethod]
    public void DetectMode_NoPrompt_KeepsCurrentMode()
    {
        Apple1PromptDetector.DetectMode("hello world", Apple1TerminalMode.Basic)
            .Should().Be(Apple1TerminalMode.Basic);
        Apple1PromptDetector.DetectMode("some output", Apple1TerminalMode.WozMonitor)
            .Should().Be(Apple1TerminalMode.WozMonitor);
    }

    [TestMethod]
    public void DetectMode_UnknownOrBootingWithBackslash_FallsBackToWoz()
    {
        Apple1PromptDetector.DetectMode("some \\ text", Apple1TerminalMode.Unknown)
            .Should().Be(Apple1TerminalMode.WozMonitor);
        Apple1PromptDetector.DetectMode("some > text", Apple1TerminalMode.Booting)
            .Should().Be(Apple1TerminalMode.WozMonitor);
    }
}
