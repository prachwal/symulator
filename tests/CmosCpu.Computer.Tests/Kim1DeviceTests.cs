using CmosCpu.Computer;
using CmosCpu.Core;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class Kim1DeviceTests
{
    internal const string Kim1Profile = """
    {
        "id": "kim-1",
        "name": "KIM-1",
        "cpu": "mos6502",
        "clockHz": 1000000,
        "memory": {
            "ram": [{ "start": "0x0000", "size": "0x0400" }],
            "rom": [
                { "start": "0x1800", "size": "0x0400" },
                { "start": "0x1C00", "size": "0x0400" }
            ],
            "vectors": { "reset": "0x1C00", "nmi": "0x1C00", "irq": "0x1C00" }
        },
        "devices": [
            { "type": "kim1-6530-io", "id": "riot6530", "start": "0x1700", "size": "0x0100" },
            { "type": "kim1-led-display", "id": "display", "digits": 6 },
            { "type": "kim1-keypad", "id": "keypad" }
        ]
    }
    """;

    [TestMethod]
    public void Kim1Io_Handles_1700_To_17FF()
    {
        var riot = new Kim1Riot6530IoDevice();

        riot.Handles(0x1700).Should().BeTrue();
        riot.Handles(0x17FF).Should().BeTrue();
        riot.Handles(0x16FF).Should().BeFalse();
        riot.Handles(0x1800).Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Io_PortA_WithDDR_ReturnsWrittenValue()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1701, 0xFF); // DDRA = all outputs
        riot.Write(0x1700, 0xAB);

        riot.Read(0x1700).Should().Be(0xAB);
    }

    [TestMethod]
    public void Kim1Io_DDR_RegisterRoundtrip()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1701, 0xAB);
        riot.Read(0x1701).Should().Be(0xAB);
    }

    [TestMethod]
    public void Kim1Io_Version_Increments_OnWriteChange()
    {
        var riot = new Kim1Riot6530IoDevice();

        long v0 = riot.Version;
        riot.Write(0x1701, 0x42);
        riot.Version.Should().Be(v0 + 1);

        riot.Write(0x1701, 0x42);
        riot.Version.Should().Be(v0 + 1);

        riot.Write(0x1701, 0xFF);
        riot.Version.Should().Be(v0 + 2);
    }

    [TestMethod]
    public void Kim1Io_PortA_InputBitsComeFromExternalInput()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1701, 0xF0); // upper nibble output, lower nibble input
        riot.Write(0x1700, 0xFF); // output latch = all ones
        riot.SetPortAInput(0x0A); // lower input bits = 1010

        byte result = riot.Read(0x1700);
        result.Should().Be(0xFA); // upper = latch(FF), lower = input(0A)
    }

    [TestMethod]
    public void Kim1Io_PortB_WithDDR_ReturnsWrittenValue()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1703, 0xFF); // DDRB = all outputs
        riot.Write(0x1702, 0xCD);

        riot.Read(0x1702).Should().Be(0xCD);
    }

    [TestMethod]
    public void Kim1Io_PortB_MixedDDR()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1703, 0x0F); // lower nibble output, upper nibble input
        riot.Write(0x1702, 0xFF); // output latch = all ones
        riot.SetPortBInput(0xB0); // upper input bits = 1011

        byte result = riot.Read(0x1702);
        result.Should().Be(0xBF); // upper = input(B0), lower = latch(FF)
    }

    [TestMethod]
    public void Kim1Io_TimerWritePrescaler1_TicksCorrectly()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1704, 10); // load timer = 10, prescaler /1

        riot.Tick(5);
        riot.TimerValue.Should().Be(5);

        riot.Tick(5);
        riot.TimerValue.Should().Be(0);
        riot.TimerUnderflow.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerPrescaler8_TicksCorrectly()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1705, 5); // load timer = 5, prescaler /8

        riot.Tick(40); // 5 * 8 = 40 cycles to underflow
        riot.TimerValue.Should().Be(0);
        riot.TimerUnderflow.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerPrescaler64_TicksCorrectly()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1706, 3); // load timer, prescaler /64

        riot.Tick(192); // 3 * 64 = 192 cycles
        riot.TimerValue.Should().Be(0);
        riot.TimerUnderflow.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerPrescaler1024_TicksCorrectly()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1707, 2); // load timer, prescaler /1024

        riot.Tick(2048); // 2 * 1024 = 2048 cycles
        riot.TimerValue.Should().Be(0);
        riot.TimerUnderflow.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerUnderflow_SetsIrqPending_WhenIrqEnabled()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 5); // load timer=5, prescaler /1, IRQ enabled

        riot.Tick(5);
        riot.TimerUnderflow.Should().BeTrue();
        riot.IrqPending.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerUnderflow_DoesNotSetIrqPending_WhenIrqDisabled()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1704, 5); // load timer=5, prescaler /1, IRQ NOT enabled

        riot.Tick(5);
        riot.TimerUnderflow.Should().BeTrue();
        riot.IrqPending.Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Io_ReadTimerFlagRegister_ClearsUnderflowAndIrq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 5);
        riot.Tick(5);
        riot.TimerUnderflow.Should().BeTrue();

        riot.Read(0x1705);
        riot.TimerUnderflow.Should().BeFalse();
        riot.IrqPending.Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Io_TimerWrite_AfterUnderflow_RestartsTimer()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1704, 2);
        riot.Tick(2);
        riot.TimerUnderflow.Should().BeTrue();

        riot.Write(0x1704, 10);
        riot.TimerUnderflow.Should().BeFalse();
        riot.TimerValue.Should().Be(10);
    }

    [TestMethod]
    public void Kim1Io_IrqEnable_WriteTo08_EnablesIrq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.IrqEnabled.Should().BeFalse();

        riot.Write(0x1708, 10);
        riot.IrqEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_IrqDisable_WriteTo09_DisablesIrq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 10);
        riot.IrqEnabled.Should().BeTrue();

        riot.Write(0x1709, 0);
        riot.IrqEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Keypad_PressKey_AddsKey()
    {
        var keypad = new Kim1KeypadState();

        keypad.PressKey("A");

        keypad.PressedKeys.Should().Contain("A");
        keypad.PressedKeys.Count.Should().Be(1);
    }

    [TestMethod]
    public void Kim1Keypad_ReleaseKey_RemovesKey()
    {
        var keypad = new Kim1KeypadState();

        keypad.PressKey("A");
        keypad.ReleaseKey("A");

        keypad.PressedKeys.Should().NotContain("A");
        keypad.PressedKeys.Should().BeEmpty();
    }

    [TestMethod]
    public void Kim1LedDisplay_DefaultState_IsSixDashes()
    {
        var display = new Kim1LedDisplayState();

        display.Digits.Should().Be("------");
    }

    [TestMethod]
    public void Kim1Profile_LoadsWithoutUnknownDeviceException()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Kim1Profile);

        machine.Should().NotBeNull();
        machine.Profile.Id.Should().Be("kim-1");
        machine.Kim1Riot.Should().NotBeNull();
        machine.Kim1Riot003.Should().BeNull();
        machine.Kim1LedDisplay.Should().NotBeNull();
        machine.Kim1Keypad.Should().NotBeNull();
    }

    [TestMethod]
    public void ComputerMachine_MapsKim1IoRange()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Kim1Profile);

        machine.Kim1Riot!.Write(0x1701, 0xFF); // DDRA all outputs
        machine.Memory.WriteByte(0x1700, 0x55);

        machine.Memory.ReadByte(0x1700).Should().Be(0x55);

        machine.Kim1Riot.Write(0x1703, 0xFF); // DDRB all outputs
        machine.Memory.WriteByte(0x1702, 0xAA);
        machine.Memory.ReadByte(0x1702).Should().Be(0xAA);
    }

    [TestMethod]
    public void Kim1Keypad_OnlyValidKeysAccepted()
    {
        var keypad = new Kim1KeypadState();

        keypad.PressKey("AD");
        keypad.PressKey("GO");
        keypad.PressKey("INVALID");

        keypad.PressedKeys.Should().Contain("AD");
        keypad.PressedKeys.Should().Contain("GO");
        keypad.PressedKeys.Count.Should().Be(2);
    }

    [TestMethod]
    public void Kim1LedDisplay_Update_ChangesDigits()
    {
        var display = new Kim1LedDisplayState();

        display.Update("00FF00");

        display.Digits.Should().Be("00FF00");
    }
}
