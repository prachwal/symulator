using CmosCpu.Computer.Devices;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class Hd44780LcdTests
{
    private static Hd44780Lcd Create() => new();
    private ulong _tickCounter;

    private void Wait(Hd44780Lcd lcd)
    {
        _tickCounter += 1_000_000;
        lcd.Tick(_tickCounter);
    }

    private void Write(Hd44780Lcd lcd, byte value)
    {
        lcd.WriteData(value);
        Wait(lcd);
    }

    private void Cmd(Hd44780Lcd lcd, byte cmd)
    {
        lcd.ExecuteInstruction(cmd);
        Wait(lcd);
    }

    [TestMethod]
    public void Reset_ClearsDdramAndMapsAddressZero()
    {
        var lcd = Create();
        lcd.Reset();
        lcd.Ddram[0].Should().Be(0);
        lcd.AddressCounter.Should().Be(0);
        lcd.DisplayOn.Should().BeFalse();
    }

    [TestMethod]
    public void ClearDisplay_ZeroesDdram()
    {
        var lcd = Create();
        Write(lcd, (byte)'A');
        Write(lcd, (byte)'B');
        lcd.Ddram[0].Should().Be((byte)'A');
        lcd.Ddram[1].Should().Be((byte)'B');

        Cmd(lcd, 0x01);
        lcd.Ddram[0].Should().Be(0);
        lcd.Ddram[1].Should().Be(0);
    }

    [TestMethod]
    public void WriteData_AfterSetDdramAddress_WritesToCorrectLocation()
    {
        var lcd = Create();
        Cmd(lcd, 0x80);
        lcd.WriteData(0x41);
        lcd.Ddram[0].Should().Be(0x41);
    }

    [TestMethod]
    public void WriteData_AddressIncrementsByDefault()
    {
        var lcd = Create();
        Cmd(lcd, 0x80);
        Write(lcd, (byte)'A');
        Write(lcd, (byte)'B');
        lcd.Ddram[0].Should().Be((byte)'A');
        lcd.Ddram[1].Should().Be((byte)'B');
    }

    [TestMethod]
    public void EntryModeSet_Decrement()
    {
        var lcd = Create();
        Cmd(lcd, 0x04);
        Cmd(lcd, 0x85);
        lcd.WriteData((byte)'A');
        lcd.AddressCounter.Should().Be(4);
    }

    [TestMethod]
    public void FunctionSet_8Bit2Line()
    {
        var lcd = Create();
        Cmd(lcd, 0x38);
        lcd.EightBitMode.Should().BeTrue();
        lcd.TwoLineMode.Should().BeTrue();
    }

    [TestMethod]
    public void ReadStatus_ReturnsAddressCounter()
    {
        var lcd = Create();
        Cmd(lcd, 0x85);
        byte status = lcd.ReadStatus();
        (status & 0x7F).Should().Be(5);
    }

    [TestMethod]
    public void ReadData_Pipeline()
    {
        var lcd = Create();
        Cmd(lcd, 0x80);
        Write(lcd, (byte)'X');
        Cmd(lcd, 0x81);
        Write(lcd, (byte)'Y');
        Cmd(lcd, 0x80);
        byte v1 = lcd.ReadData();
        v1.Should().Be((byte)'X');
        byte v2 = lcd.ReadData();
        v2.Should().Be((byte)'Y');
    }

    [TestMethod]
    public void DisplayOnOff_ControlsVisibility()
    {
        var lcd = Create();
        Cmd(lcd, 0x08);
        lcd.DisplayOn.Should().BeFalse();

        Cmd(lcd, 0x0C);
        lcd.DisplayOn.Should().BeTrue();
        lcd.CursorOn.Should().BeFalse();

        Cmd(lcd, 0x0F);
        lcd.DisplayOn.Should().BeTrue();
        lcd.CursorOn.Should().BeTrue();
        lcd.BlinkOn.Should().BeTrue();
    }

    [TestMethod]
    public void ShiftDisplay_Right_RotatesDdram()
    {
        var lcd = Create();
        Cmd(lcd, 0x80);
        Write(lcd, (byte)'1');
        Write(lcd, (byte)'2');

        Cmd(lcd, 0x1C);
        lcd.Ddram[1].Should().Be((byte)'1');
        lcd.Ddram[2].Should().Be((byte)'2');
    }

    [TestMethod]
    public void WriteCgram_ReadsBack()
    {
        var lcd = Create();
        Cmd(lcd, 0x40);
        Write(lcd, 0x0A);
        Write(lcd, 0x15);
        lcd.Cgram[0].Should().Be(0x0A);
        lcd.Cgram[1].Should().Be(0x15);
    }

    [TestMethod]
    public void BusyFlag_ClearsAfterTick()
    {
        var lcd = Create();
        lcd.ExecuteInstruction(0x01);
        lcd.IsBusy.Should().BeTrue();
        Wait(lcd);
        lcd.IsBusy.Should().BeFalse();
    }

    [TestMethod]
    public void BusyFlag_IgnoresWritesWhileBusy()
    {
        var lcd = Create();
        lcd.ExecuteInstruction(0x01); // Clear Display — long busy
        lcd.IsBusy.Should().BeTrue();

        // Write while busy — MUST be ignored (no Wait between)
        lcd.WriteData((byte)'X');
        lcd.Ddram[0].Should().Be(0, "write while busy should be ignored");

        Wait(lcd);
        lcd.Ddram[0].Should().Be(0, "after busy clears, DDRAM should still be empty");
    }

    [TestMethod]
    public void GetSnapshot_ReturnsConsistentState()
    {
        var lcd = Create();
        Cmd(lcd, 0x38);
        Cmd(lcd, 0x0C);
        Cmd(lcd, 0x80);
        lcd.WriteData((byte)'H');

        var snap = lcd.GetSnapshot();
        snap.Ddram[0].Should().Be((byte)'H');
        snap.DisplayOn.Should().BeTrue();
        snap.EightBitMode.Should().BeTrue();
        snap.TwoLineMode.Should().BeTrue();
    }
}
