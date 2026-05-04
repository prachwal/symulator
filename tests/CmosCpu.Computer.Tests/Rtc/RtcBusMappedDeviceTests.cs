using CmosCpu.Computer.Devices.Rtc;
using FluentAssertions;

namespace CmosCpu.Computer.Tests.Rtc;

[TestClass]
public sealed class RtcBusMappedDeviceTests
{
    [TestMethod]
    public void LinearRead_MapsAddressToRegister()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2024, 1, 2, 3, 4, 5));
        var device = new RtcBusMappedDevice(clock, 0xD100, RtcBusMode.Linear);

        device.Read(0xD100).Should().Be(0x05);
        device.Read(0xD101).Should().Be(0x04);
        device.Read(0xD102).Should().Be(0x03);
    }

    [TestMethod]
    public void LinearWrite_MapsAddressToRegister()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2024, 1, 2, 3, 4, 5));
        var device = new RtcBusMappedDevice(clock, 0xD100, RtcBusMode.Linear);

        device.Write(0xD100, 0x55);
        device.Write(0xD101, 0x44);

        clock.CurrentTime.Second.Should().Be(55);
        clock.CurrentTime.Minute.Should().Be(44);
    }

    [TestMethod]
    public void IndexedRead_UsesIndexAndDataPorts()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2024, 1, 2, 3, 4, 5));
        var device = new RtcBusMappedDevice(clock, 0xD100, RtcBusMode.Indexed, size: 2);

        device.Write(0xD100, RtcClockCore.MinutesRegister);

        device.Read(0xD100).Should().Be(RtcClockCore.MinutesRegister);
        device.Read(0xD101).Should().Be(0x04);
    }

    [TestMethod]
    public void IndexedWrite_UsesIndexAndDataPorts()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2024, 1, 2, 3, 4, 5));
        var device = new RtcBusMappedDevice(clock, 0xD100, RtcBusMode.Indexed, size: 2);

        device.Write(0xD100, RtcClockCore.SecondsRegister);
        device.Write(0xD101, 0x45);

        clock.CurrentTime.Second.Should().Be(45);
    }

    [TestMethod]
    public void OutOfRangeAddress_IsNotHandled()
    {
        var clock = new RtcClockCore();
        var device = new RtcBusMappedDevice(clock, 0xD100, RtcBusMode.Linear, size: 0x10);

        device.Handles(0xD0FF).Should().BeFalse();
        device.Handles(0xD110).Should().BeFalse();
        device.Read(0xD110).Should().Be(0xFF);
    }

    [TestMethod]
    public void Snapshot_ShouldIncludeDirectBusMetadata()
    {
        var clock = new RtcClockCore();
        var device = new RtcBusMappedDevice(clock, 0xD100, RtcBusMode.Indexed, size: 2);

        var snapshot = device.CreateSnapshot();

        snapshot.DirectBusBaseAddress.Should().Be(0xD100);
        snapshot.DirectBusMode.Should().Be(RtcBusMode.Indexed);
    }
}
