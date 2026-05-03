using FluentAssertions;
using Symulator.Machines.Apple1.Devices;

namespace Symulator.Machines.Apple1.Tests;

[TestClass]
public sealed class Apple1TerminalBufferTests
{
    [TestMethod]
    public void Buffer_DefaultSize_40x24()
    {
        var buf = new Apple1TerminalBuffer();
        buf.Columns.Should().Be(40);
        buf.Rows.Should().Be(24);
    }

    [TestMethod]
    public void Write_Char_StoredInCell()
    {
        var buf = new Apple1TerminalBuffer();
        buf.Write((byte)'A');

        buf.Cells[0, 0].Should().Be('A');
        buf.CursorColumn.Should().Be(1);
        buf.CursorRow.Should().Be(0);
    }

    [TestMethod]
    public void Write_CR_MovesToNextLine()
    {
        var buf = new Apple1TerminalBuffer();
        buf.Write((byte)'X');
        buf.Write(0x0D); // CR

        buf.CursorRow.Should().Be(1);
        buf.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void Write_ControlChar_Ignored()
    {
        var buf = new Apple1TerminalBuffer();
        buf.Write(0x01);
        buf.Write(0x02);

        buf.Cells[0, 0].Should().Be(' ');
        buf.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void Write_40Chars_WrapsToNextRow()
    {
        var buf = new Apple1TerminalBuffer(40, 2);
        for (int i = 0; i < 40; i++)
            buf.Write((byte)'X');

        buf.CursorRow.Should().Be(1);
        buf.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void Scroll_WhenExceedsRows()
    {
        var buf = new Apple1TerminalBuffer(10, 2);

        // Fill row 0 with 10 chars, wrapping to row 1
        FillLine(buf, 'A', 10);
        // CR moves to row 1
        buf.Write(0x0D);
        // Fill row 1, wrapping past last row → scrolls, row 0 = "A"s
        FillLine(buf, 'B', 10);
        // Now at row 1, fill more → wraps past last row → scroll again
        FillLine(buf, 'C', 1);
        // Writing 'C' at col 0 of row 1 wraps past last row → scroll

        // After two scrolls: first line "A" should be gone
        buf.Cells[0, 0].Should().Be('B', "first line scrolled off");
    }

    [TestMethod]
    public void Cr_AtLastRow_StaysOnLastRow()
    {
        var buf = new Apple1TerminalBuffer(10, 2);
        FillLine(buf, 'X', 10); // wraps to row 1
        buf.Write(0x0D); // moves to row 1
        FillLine(buf, 'Y', 10); // wraps past row 1 → scrolls, row 0 = X, row 1 = empty
        buf.Write(0x0D); // at last row, CR stays at Rows-1

        buf.CursorRow.Should().Be(1);
        buf.CursorColumn.Should().Be(0);
    }

    private static void FillLine(Apple1TerminalBuffer buf, char ch, int count)
    {
        for (int i = 0; i < count; i++)
            buf.Write((byte)ch);
    }

    [TestMethod]
    public void Clear_ResetsAll()
    {
        var buf = new Apple1TerminalBuffer();
        buf.Write((byte)'A');
        buf.Clear();

        buf.Cells[0, 0].Should().Be(' ');
        buf.CursorRow.Should().Be(0);
        buf.CursorColumn.Should().Be(0);
    }

    [TestMethod]
    public void Bit7_Stripped()
    {
        var buf = new Apple1TerminalBuffer();
        buf.Write(0xC1); // 'A' with bit 7 set

        buf.Cells[0, 0].Should().Be('A');
    }
}

[TestClass]
public sealed class Apple1PiaWiringTests
{
    [TestMethod]
    public void QueueKey_SetsPendingKey()
    {
        var pia = new CmosCpu.Computer.Devices.Pia6821(0xD014, 0xD017);
        var buf = new Apple1TerminalBuffer();
        var wiring = new Apple1PiaWiring(pia, buf);

        wiring.QueueKey('A');

        // PIA Port A input should reflect the key with bit 7 set
        pia.PortAInput.Should().Be(0xC1, "key 'A' with bit7 set");
    }

    [TestMethod]
    public void PortBOutput_WritesToTerminalBuffer()
    {
        var pia = new CmosCpu.Computer.Devices.Pia6821(0xD014, 0xD017);
        var buf = new Apple1TerminalBuffer();
        var wiring = new Apple1PiaWiring(pia, buf);

        // Simulate CPU writing to PIA Port B
        // First set DDRB = $FF (all outputs) via CRB bit2=0 → DDR mode
        pia.Write(0xD017, 0x00); // CRB bit2=0 → DDR
        pia.Write(0xD016, 0xFF); // DDRB = FF

        // Switch to data mode
        pia.Write(0xD017, 0x04); // CRB bit2=1 → data mode
        pia.Write(0xD016, (byte)'H'); // write 'H' to Port B data

        buf.Cells[0, 0].Should().Be('H');
    }

    [TestMethod]
    public void NoInput_NoRandomBytes()
    {
        var pia = new CmosCpu.Computer.Devices.Pia6821(0xD014, 0xD017);
        var buf = new Apple1TerminalBuffer();
        var wiring = new Apple1PiaWiring(pia, buf);

        // Without any key queued, PIA input should be 0
        pia.PortAInput.Should().Be(0);
    }
}
