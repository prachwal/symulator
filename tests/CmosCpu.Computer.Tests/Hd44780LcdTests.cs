using CmosCpu.Computer.Devices;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class Hd44780LcdTests
{
    [TestMethod]
    public void Reset_ClearsDdramAndMapsAddressZero()
    {
        var lcd = new Hd44780Lcd(0xD020);
        lcd.Reset();

        lcd.Ddram[0].Should().Be(0, "DDRAM should be zeroed after reset");
        lcd.CursorAddr.Should().Be(0);
        lcd.DisplayOn.Should().BeFalse();
    }

    [TestMethod]
    public void WriteCommand_ClearDisplay_ZeroesDdram()
    {
        var lcd = new Hd44780Lcd(0xD020);

        // Write some data
        lcd.Write(0xD020, 0x80); // Set DDRAM addr 0
        lcd.Write(0xD021, (byte)'A');
        lcd.Write(0xD020, 0x81); // Set DDRAM addr 1
        lcd.Write(0xD021, (byte)'B');

        lcd.Ddram[0].Should().Be((byte)'A');
        lcd.Ddram[1].Should().Be((byte)'B');

        // Clear
        lcd.Write(0xD020, 0x01);

        lcd.Ddram[0].Should().Be(0, "DDRAM should be cleared");
        lcd.Ddram[1].Should().Be(0, "DDRAM should be cleared");
    }

    [TestMethod]
    public void WriteData_AfterSetDdramAddress_WritesToCorrectLocation()
    {
        var lcd = new Hd44780Lcd(0xD020);

        lcd.Write(0xD020, 0x80); // Set DDRAM addr 0
        lcd.Write(0xD021, 0x41); // Write 'A'

        lcd.Ddram[0].Should().Be(0x41);
    }

    [TestMethod]
    public void WriteData_AddressAutoIncrements()
    {
        var lcd = new Hd44780Lcd(0xD020);

        lcd.Write(0xD020, 0x80); // Set DDRAM addr 0
        lcd.Write(0xD021, (byte)'A');
        lcd.Write(0xD021, (byte)'B');

        lcd.Ddram[0].Should().Be((byte)'A');
        lcd.Ddram[1].Should().Be((byte)'B');
    }

    [TestMethod]
    public void WriteFunctionSet_8Bit2Line_SetsMode()
    {
        var lcd = new Hd44780Lcd(0xD020);

        lcd.Write(0xD020, 0x28); // Function Set: 8-bit, 2-line, 5x7

        // Cannot access private fields, but can verify effect via behavior
        // Write to second line DDRAM address to verify 2-line mode
        lcd.Write(0xD020, 0xC0); // Set DDRAM addr 0x40 (second line start)
        lcd.Write(0xD021, (byte)'X');
        lcd.Ddram[0x40].Should().Be((byte)'X');
    }

    [TestMethod]
    public void ReadStatus_ReturnsAddressCounter()
    {
        var lcd = new Hd44780Lcd(0xD020);

        lcd.Write(0xD020, 0x85); // Set DDRAM addr 5
        byte status = lcd.Read(0xD020); // RS=0 → status read

        (status & 0x7F).Should().Be(5, "address counter should be 5");
    }

    [TestMethod]
    public void ReadData_AfterAddressSet_FirstReadReturnsOldLatch()
    {
        var lcd = new Hd44780Lcd(0xD020);

        // Write 'X' at addr 0
        lcd.Write(0xD020, 0x80); // Set DDRAM addr 0
        lcd.Write(0xD021, (byte)'X');

        // Write 'Y' at addr 1
        lcd.Write(0xD020, 0x81); // Set DDRAM addr 1
        lcd.Write(0xD021, (byte)'Y');

        // Now set address and read
        lcd.Write(0xD020, 0x80); // Set DDRAM addr 0 — latch = content of addr 0 ('X')
        lcd.Write(0xD020, 0x81); // Set DDRAM addr 1 — latch = content of addr 1 ('Y')

        // First read: returns latch (content of old address = 'Y')
        byte v1 = lcd.Read(0xD021);
        v1.Should().Be((byte)'Y', "first read after address set returns latched data");

        // Second read: returns actual content of addr 1 ('Y')
        byte v2 = lcd.Read(0xD021);
        v2.Should().Be((byte)'Y', "second read returns content of current address");
    }

    [TestMethod]
    public void DisplayOnOff_ControlsVisibility()
    {
        var lcd = new Hd44780Lcd(0xD020);

        lcd.Write(0xD020, 0x08); // Display Off
        lcd.DisplayOn.Should().BeFalse();

        lcd.Write(0xD020, 0x0C); // Display On, cursor off, blink off
        lcd.DisplayOn.Should().BeTrue();
        lcd.CursorOn.Should().BeFalse();
        lcd.BlinkOn.Should().BeFalse();

        lcd.Write(0xD020, 0x0F); // Display On, cursor on, blink on
        lcd.DisplayOn.Should().BeTrue();
        lcd.CursorOn.Should().BeTrue();
        lcd.BlinkOn.Should().BeTrue();
    }

    [TestMethod]
    public void EntryModeSet_IncrementDirection()
    {
        var lcd = new Hd44780Lcd(0xD020);

        lcd.Write(0xD020, 0x04); // Entry mode: decrement, no shift
        lcd.Write(0xD020, 0x85); // Set DDRAM addr 5
        lcd.Write(0xD021, (byte)'A');

        lcd.CursorAddr.Should().Be(4, "address should decrement from 5 to 4");
    }

    [TestMethod]
    public void ShiftDisplay_Right_RotatesDdramLine()
    {
        var lcd = new Hd44780Lcd(0xD020);

        // Write row 0 content
        lcd.Write(0xD020, 0x80); // DDRAM addr 0
        lcd.Write(0xD021, (byte)'1');
        lcd.Write(0xD021, (byte)'2');

        lcd.Ddram[0].Should().Be((byte)'1');
        lcd.Ddram[1].Should().Be((byte)'2');

        // Shift display right
        lcd.Write(0xD020, 0x1C); // S/C=1, R/L=1

        // After right shift, content moves right by 1
        lcd.Ddram[1].Should().Be((byte)'1', "after right shift, old pos 0 moves to pos 1");
        lcd.Ddram[2].Should().Be((byte)'2', "after right shift, old pos 1 moves to pos 2");
    }

    [TestMethod]
    public void WriteCgramData_ReadsBackCorrectly()
    {
        var lcd = new Hd44780Lcd(0xD020);

        lcd.Write(0xD020, 0x40); // Set CGRAM addr 0
        lcd.Write(0xD021, 0x0A); // Custom pattern row 0
        lcd.Write(0xD021, 0x15); // Custom pattern row 1

        lcd.Cgram[0].Should().Be(0x0A);
        lcd.Cgram[1].Should().Be(0x15);
    }

    [TestMethod]
    public void DisplayChanged_FiresOnDdramWrite()
    {
        var lcd = new Hd44780Lcd(0xD020);
        int fireCount = 0;
        lcd.DisplayChanged += _ => fireCount++;

        lcd.Write(0xD020, 0x80); // Set DDRAM addr
        lcd.Write(0xD021, (byte)'X'); // Write data

        fireCount.Should().Be(2, "DisplayChanged fires on address set and data write");
    }
}
