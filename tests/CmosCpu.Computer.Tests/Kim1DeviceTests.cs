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
    public void Kim1Io_ReadWrite_RegisterRoundtrip()
    {
        var riot = new Kim1Riot6530IoDevice();

        riot.Write(0x1700, 0xAB);
        riot.Write(0x1710, 0xCD);
        riot.Write(0x17FF, 0xEF);

        riot.Read(0x1700).Should().Be(0xAB);
        riot.Read(0x1710).Should().Be(0xCD);
        riot.Read(0x17FF).Should().Be(0xEF);
    }

    [TestMethod]
    public void Kim1Io_Version_Increments_OnWriteChange()
    {
        var riot = new Kim1Riot6530IoDevice();

        long v0 = riot.Version;
        riot.Write(0x1700, 0x42);
        riot.Version.Should().Be(v0 + 1);

        riot.Write(0x1700, 0x42);
        riot.Version.Should().Be(v0 + 1);

        riot.Write(0x1700, 0xFF);
        riot.Version.Should().Be(v0 + 2);
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
        machine.Kim1LedDisplay.Should().NotBeNull();
        machine.Kim1Keypad.Should().NotBeNull();
    }

    [TestMethod]
    public void ComputerMachine_MapsKim1IoRange()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Kim1Profile);

        machine.Memory.WriteByte(0x1700, 0x55);
        machine.Memory.WriteByte(0x17FF, 0xAA);

        machine.Memory.ReadByte(0x1700).Should().Be(0x55);
        machine.Memory.ReadByte(0x17FF).Should().Be(0xAA);
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
