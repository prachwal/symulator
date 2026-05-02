using CmosCpu.Terminal.Tui;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class TerminalTextRingBufferTests
{
    [TestMethod]
    public void AppendShortText_GetTextReturnsIt()
    {
        var buf = new TerminalTextRingBuffer(100);
        buf.Append("HELLO");
        buf.GetText().Should().Be("HELLO");
        buf.Length.Should().Be(5);
    }

    [TestMethod]
    public void AppendMultipleTimes_Accumulates()
    {
        var buf = new TerminalTextRingBuffer(100);
        buf.Append("AB");
        buf.Append("CD");
        buf.GetText().Should().Be("ABCD");
    }

    [TestMethod]
    public void ExceedingMaxSize_DropsOldestData()
    {
        var buf = new TerminalTextRingBuffer(5);
        buf.Append("ABCDE");
        buf.Length.Should().Be(5);
        buf.GetText().Should().Be("ABCDE");

        buf.Append("F");
        buf.Length.Should().Be(5);
        buf.GetText().Should().Be("BCDEF");
    }

    [TestMethod]
    public void ExceedingMaxSizeByLargeAmount_DropsCorrectly()
    {
        var buf = new TerminalTextRingBuffer(4);
        buf.Append("ABCDEFGH");
        buf.Length.Should().Be(4);
        buf.GetText().Should().Be("EFGH");
    }

    [TestMethod]
    public void Clear_ResetsState()
    {
        var buf = new TerminalTextRingBuffer(100);
        buf.Append("SOMETHING");
        buf.Clear();
        buf.Length.Should().Be(0);
        buf.GetText().Should().BeEmpty();
    }

    [TestMethod]
    public void AppendAfterClear_Works()
    {
        var buf = new TerminalTextRingBuffer(100);
        buf.Append("OLD");
        buf.Clear();
        buf.Append("NEW");
        buf.GetText().Should().Be("NEW");
    }

    [TestMethod]
    public void EmptyBuffer_GetTextIsEmpty()
    {
        var buf = new TerminalTextRingBuffer(100);
        buf.GetText().Should().BeEmpty();
    }

    [TestMethod]
    public void AppendEmptyString_DoesNothing()
    {
        var buf = new TerminalTextRingBuffer(100);
        buf.Append("");
        buf.Length.Should().Be(0);
    }

    [TestMethod]
    public void MaxChars_DefaultsTo65536()
    {
        var buf = new TerminalTextRingBuffer();
        buf.MaxChars.Should().Be(65536);
    }

    [TestMethod]
    public void MaxChars_AtLeast1()
    {
        var buf = new TerminalTextRingBuffer(0);
        buf.MaxChars.Should().Be(65536);
    }

    [TestMethod]
    public void RingBehavior_WrapsAround()
    {
        var buf = new TerminalTextRingBuffer(3);
        buf.Append("123");
        buf.GetText().Should().Be("123");

        buf.Append("45");
        buf.GetText().Should().Be("345");
    }
}
