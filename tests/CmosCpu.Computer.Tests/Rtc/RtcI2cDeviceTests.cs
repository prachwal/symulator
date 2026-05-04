using CmosCpu.Computer.Devices.I2c;
using CmosCpu.Computer.Devices.Rtc;
using FluentAssertions;

namespace CmosCpu.Computer.Tests.Rtc;

[TestClass]
public sealed class RtcI2cDeviceTests
{
    [TestMethod]
    public void I2cWriteSingleByte_SetsRegisterPointer()
    {
        var clock = new RtcClockCore();
        var device = new RtcI2cDevice(clock);

        device.BeginWrite();
        device.WriteByte(RtcClockCore.MinutesRegister);

        device.RegisterPointer.Should().Be(RtcClockCore.MinutesRegister);
    }

    [TestMethod]
    public void I2cSequentialRead_ReturnsTimeRegisters()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2024, 1, 2, 3, 4, 5));
        var device = new RtcI2cDevice(clock);

        device.WriteBytes(0x00);
        var values = device.ReadBytes(7);

        values.Should().Equal(0x05, 0x04, 0x03, 0x03, 0x02, 0x01, 0x24);
    }

    [TestMethod]
    public void I2cSequentialWrite_UpdatesRegisters()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2024, 1, 2, 3, 4, 5));
        var device = new RtcI2cDevice(clock);

        device.WriteBytes(0x00, 0x55, 0x44, 0x12);

        clock.CurrentTime.Second.Should().Be(55);
        clock.CurrentTime.Minute.Should().Be(44);
        clock.CurrentTime.Hour.Should().Be(12);
    }

    [TestMethod]
    public void I2cRead_AutoIncrementsPointer()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2024, 1, 2, 3, 4, 5));
        var device = new RtcI2cDevice(clock);

        device.WriteBytes(RtcClockCore.SecondsRegister);

        device.ReadByte().Should().Be(0x05);
        device.RegisterPointer.Should().Be(RtcClockCore.MinutesRegister);
        device.ReadByte().Should().Be(0x04);
    }

    [TestMethod]
    public void I2cRead_WrapsPointer()
    {
        var clock = new RtcClockCore();
        var device = new RtcI2cDevice(clock);

        device.WriteBytes(0x3F);
        device.ReadByte();

        device.RegisterPointer.Should().Be(0x00);
    }

    [TestMethod]
    public void I2cBus_ShouldRouteToRtcDevice()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2024, 1, 2, 3, 4, 5));
        var device = new RtcI2cDevice(clock);
        var bus = new I2cBus();
        bus.Attach(device);

        bus.WriteByte(0x68, 0x00).Should().BeTrue();
        bus.TryReadByte(0x68, out var value).Should().BeTrue();

        value.Should().Be(0x05);
    }

    [TestMethod]
    public void Snapshot_ShouldIncludeI2cAddress()
    {
        var clock = new RtcClockCore();
        var device = new RtcI2cDevice(clock, 0x68);

        var snapshot = device.CreateSnapshot();

        snapshot.I2cAddress.Should().Be(0x68);
    }
}
