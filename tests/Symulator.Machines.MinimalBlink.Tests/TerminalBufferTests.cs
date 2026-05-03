using FluentAssertions;
using Symulator.Application.Terminal;

namespace Symulator.Machines.MinimalBlink.Tests;

[TestClass]
public sealed class TerminalBufferTests
{
    [TestMethod]
    public void WriteByte_ShouldAppendPrintableCharacters()
    {
        var term = new TerminalBuffer();
        term.WriteByte((byte)'H');
        term.WriteByte((byte)'i');
        term.GetSnapshot().CurrentLine.Should().Be("Hi");
    }

    [TestMethod]
    public void WriteByte_ShouldCommitLine_OnLineFeed()
    {
        var term = new TerminalBuffer();
        term.WriteByte((byte)'H');
        term.WriteByte(0x0A);
        var snap = term.GetSnapshot();
        snap.Lines.Should().Contain("H");
        snap.CurrentLine.Should().BeEmpty();
    }

    [TestMethod]
    public void WriteByte_ShouldHandleBackspace()
    {
        var term = new TerminalBuffer();
        term.WriteByte((byte)'A');
        term.WriteByte((byte)'B');
        term.WriteByte(0x08);
        term.GetSnapshot().CurrentLine.Should().Be("A");
    }

    [TestMethod]
    public void Clear_ShouldResetState()
    {
        var term = new TerminalBuffer();
        term.WriteByte((byte)'H');
        term.Clear();
        term.GetSnapshot().CurrentLine.Should().BeEmpty();
    }

    [TestMethod]
    public void WriteString_ShouldHandleMultipleLines()
    {
        var term = new TerminalBuffer();
        term.WriteString("Hello\nWorld\n");
        var snap = term.GetSnapshot();
        snap.Lines.Should().Contain("Hello");
        snap.Lines.Should().Contain("World");
    }
}
