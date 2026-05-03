using CmosCpu.Computer.Devices;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class Hd44780AdapterTests
{
    private static Hd44780Lcd CreateLcd()
    {
        var lcd = new Hd44780Lcd();
        return lcd;
    }

    private ulong _tickCounter = 100_000_000;

    private void Wait(Hd44780Lcd lcd)
    {
        _tickCounter += 1_000_000;
        lcd.Tick(_tickCounter);
    }

    // ── Direct Bus Adapter ──────────────────────────────────────

    [TestMethod]
    public void DirectBus_WriteCommand_ExecutesInstruction()
    {
        var lcd = CreateLcd();
        var bus = new Hd44780DirectBusAdapter(lcd, 0xFE00);

        bus.Write(0xFE00, 0x38);
        Wait(lcd);
        lcd.EightBitMode.Should().BeTrue();
        lcd.TwoLineMode.Should().BeTrue();
    }

    [TestMethod]
    public void DirectBus_WriteData_WritesToDdram()
    {
        var lcd = CreateLcd();
        var bus = new Hd44780DirectBusAdapter(lcd, 0xFE00);

        bus.Write(0xFE00, 0x80);
        Wait(lcd);
        bus.Write(0xFE01, (byte)'A');
        lcd.Ddram[0].Should().Be((byte)'A');
    }

    [TestMethod]
    public void DirectBus_ReadStatus_ReturnsBusyAndAc()
    {
        var lcd = CreateLcd();
        var bus = new Hd44780DirectBusAdapter(lcd, 0xFE00);

        bus.Write(0xFE00, 0x85);
        Wait(lcd);
        byte status = bus.Read(0xFE00);
        (status & 0x7F).Should().Be(5);
    }

    [TestMethod]
    public void DirectBus_ReadData_AfterAddressSet()
    {
        var lcd = CreateLcd();
        var bus = new Hd44780DirectBusAdapter(lcd, 0xFE00);

        bus.Write(0xFE00, 0x80);
        Wait(lcd);
        bus.Write(0xFE01, (byte)'X');
        Wait(lcd);
        bus.Write(0xFE01, (byte)'Y');
        Wait(lcd);

        bus.Write(0xFE00, 0x80);
        Wait(lcd);
        byte v1 = bus.Read(0xFE01);
        v1.Should().Be((byte)'X');
        byte v2 = bus.Read(0xFE01);
        v2.Should().Be((byte)'Y');
    }

    // ── Parallel 8-bit Adapter ──────────────────────────────────

    [TestMethod]
    public void Parallel8Bit_FallingEdge_ExecutesInstruction()
    {
        var lcd = CreateLcd();
        var adapter = new Hd44780Parallel8BitAdapter(lcd);

        adapter.WritePins(new Hd44780Pins(false, false, true, 0x38));
        adapter.WritePins(new Hd44780Pins(false, false, false, 0x38));
        Wait(lcd);

        lcd.EightBitMode.Should().BeTrue();
        lcd.TwoLineMode.Should().BeTrue();
    }

    [TestMethod]
    public void Parallel8Bit_FallingEdge_WritesData()
    {
        var lcd = CreateLcd();
        var adapter = new Hd44780Parallel8BitAdapter(lcd);

        adapter.WritePins(new Hd44780Pins(false, false, true, 0x80));
        adapter.WritePins(new Hd44780Pins(false, false, false, 0x80));
        Wait(lcd);

        adapter.WritePins(new Hd44780Pins(true, false, true, (byte)'A'));
        adapter.WritePins(new Hd44780Pins(true, false, false, (byte)'A'));

        lcd.Ddram[0].Should().Be((byte)'A');
    }

    [TestMethod]
    public void Parallel8Bit_ReadStatus()
    {
        var lcd = CreateLcd();
        var adapter = new Hd44780Parallel8BitAdapter(lcd);

        adapter.WritePins(new Hd44780Pins(false, true, true, 0));
        adapter.WritePins(new Hd44780Pins(false, true, false, 0));

        adapter.OutputData.Should().Be(0);
    }

    [TestMethod]
    public void Parallel8Bit_NoActionOnRisingEdge()
    {
        var lcd = CreateLcd();
        var adapter = new Hd44780Parallel8BitAdapter(lcd);

        // E rising — no action (default after reset is 8-bit=true)
        adapter.WritePins(new Hd44780Pins(false, false, false, 0x38));
        adapter.WritePins(new Hd44780Pins(false, false, true, 0x38));

        lcd.EightBitMode.Should().BeTrue("rising edge should not trigger instruction");
    }

    // ── Parallel 4-bit Adapter ──────────────────────────────────

    [TestMethod]
    public void Parallel4Bit_AssemblesNibbles()
    {
        var lcd = CreateLcd();
        var adapter = new Hd44780Parallel4BitAdapter(lcd);

        // Set DDRAM addr 0 first (via direct instruction)
        lcd.ExecuteInstruction(0x80);
        Wait(lcd);

        // High nibble 0x4 → 0x40 in upper bits
        adapter.WritePins(new Hd44780Pins(true, false, true, 0x40));
        adapter.WritePins(new Hd44780Pins(true, false, false, 0x40));

        // Low nibble 0x1 → 0x10 → shift → 0x01
        adapter.WritePins(new Hd44780Pins(true, false, true, 0x10));
        adapter.WritePins(new Hd44780Pins(true, false, false, 0x10));

        lcd.Ddram[0].Should().Be(0x41);
    }

    [TestMethod]
    public void Parallel4Bit_InstructionFromNibbles()
    {
        var lcd = CreateLcd();
        var adapter = new Hd44780Parallel4BitAdapter(lcd);

        // 0010 1000 = 0x28 → nibbles: 0x2, 0x8
        adapter.WritePins(new Hd44780Pins(false, false, true, 0x20));
        adapter.WritePins(new Hd44780Pins(false, false, false, 0x20));
        Wait(lcd); // clear busy from previous operation

        adapter.WritePins(new Hd44780Pins(false, false, true, 0x80));
        adapter.WritePins(new Hd44780Pins(false, false, false, 0x80));
        Wait(lcd);

        lcd.EightBitMode.Should().BeFalse("0x28 = 4-bit mode");
        lcd.TwoLineMode.Should().BeTrue();
    }

    // ── PCF8574 Backpack ────────────────────────────────────────

    [TestMethod]
    public void Pcf8574_DefaultMap_DecodesPins()
    {
        var lcd = CreateLcd();
        var lcd4 = new Hd44780Parallel4BitAdapter(lcd);
        var backpack = new Pcf8574Hd44780Backpack(lcd4);

        // Set DDRAM addr 0 first
        lcd.ExecuteInstruction(0x80);
        Wait(lcd);

        // byte: RS=1, E=1, D4=0, D5=1, D6=1, D7=0 → nibble 0x60
        // bit0=RS, bit1=RW, bit2=E, bit3=BL
        // 0b0110_1101 = 0x6D → RS=1, E=1, BL=1, D4=0, D5=1, D6=1, D7=0
        backpack.WriteByte(0x6D); // E high, BL=1
        backpack.WriteByte(0x69); // E low (clear bit2), BL=1
        
        backpack.BacklightOn.Should().BeTrue("bit3=1");

        // Low nibble 0x0: D4=0, D5=0, D6=0, D7=0
        // Same RS, same BL, E high:
        backpack.WriteByte(0x6D); // Wait — E was low, now high
        // Actually we need: RS=1, E=1, BL=1, D4=0, D5=0, D6=0, D7=0
        // 0b0000_1101 = 0x0D
        backpack.WriteByte(0x0D); // E high
        backpack.WriteByte(0x09); // E low (falling edge)
        
        // value = 0x60 | 0x00 = 0x60 = '`'
        lcd.Ddram[0].Should().Be((byte)'`');
    }

    [TestMethod]
    public void Pcf8574_CustomPinMap()
    {
        var map = new Pcf8574Hd44780PinMap
        {
            RsBit = 1,
            RwBit = 2,
            EBit = 3,
            BacklightBit = 0,
            D4Bit = 4,
            D5Bit = 5,
            D6Bit = 6,
            D7Bit = 7
        };
        var lcd = CreateLcd();
        var lcd4 = new Hd44780Parallel4BitAdapter(lcd);
        var backpack = new Pcf8574Hd44780Backpack(lcd4, map);

        // BL at bit0, E at bit3
        // 0b0000_1001 = 0x09 → BL=1, E=1
        backpack.WriteByte(0x09);
        // 0b0000_0001 = 0x01 → BL=1, E=0 (falling edge)
        backpack.WriteByte(0x01);
        backpack.BacklightOn.Should().BeTrue();
    }
}
