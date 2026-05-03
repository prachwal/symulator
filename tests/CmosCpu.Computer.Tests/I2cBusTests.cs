using CmosCpu.Computer.Devices.I2c;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

public sealed class FakeI2cDevice : II2cDevice
{
    public byte Address { get; }
    public byte LastValue { get; set; }
    public byte ReadVal { get; set; }

    public FakeI2cDevice(byte address) { Address = address; }
    public void WriteByte(byte value) { LastValue = value; }
    public byte ReadByte() => ReadVal;
}

[TestClass]
public sealed class I2cBusTests
{
    [TestMethod]
    public void WriteByte_ShouldForwardValue_WhenDeviceExists()
    {
        var bus = new I2cBus();
        var device = new FakeI2cDevice(0x27);
        bus.Attach(device);

        bool result = bus.WriteByte(0x27, 0x42);

        result.Should().BeTrue();
        device.LastValue.Should().Be(0x42);
    }

    [TestMethod]
    public void WriteByte_ShouldReturnFalse_WhenDeviceMissing()
    {
        var bus = new I2cBus();
        bus.WriteByte(0x27, 0x42).Should().BeFalse();
    }

    [TestMethod]
    public void TryReadByte_ShouldReturnValue_WhenDeviceExists()
    {
        var bus = new I2cBus();
        var device = new FakeI2cDevice(0x27) { ReadVal = 0xAA };
        bus.Attach(device);

        bool ok = bus.TryReadByte(0x27, out byte val);

        ok.Should().BeTrue();
        val.Should().Be(0xAA);
    }

    [TestMethod]
    public void TryReadByte_ShouldReturnFalse_WhenDeviceMissing()
    {
        var bus = new I2cBus();
        bus.TryReadByte(0x27, out _).Should().BeFalse();
    }
}

[TestClass]
public sealed class MemoryMappedI2cControllerTests
{
    [TestMethod]
    public void WriteCommandWriteByte_SendsDataToSelectedAddress()
    {
        var bus = new I2cBus();
        var device = new FakeI2cDevice(0x27);
        bus.Attach(device);

        var ctrl = new MemoryMappedI2cController(bus);

        ctrl.Write(1, 0x27); // address
        ctrl.Write(2, 0x55); // data
        ctrl.Write(0, MemoryMappedI2cController.CommandWriteByte);

        ctrl.Read(3).Should().Be(0x00, "status should be OK");
        device.LastValue.Should().Be(0x55);
    }

    [TestMethod]
    public void WriteCommandWriteByte_NackWhenDeviceMissing()
    {
        var bus = new I2cBus();
        var ctrl = new MemoryMappedI2cController(bus);

        ctrl.Write(1, 0x27);
        ctrl.Write(2, 0x55);
        ctrl.Write(0, MemoryMappedI2cController.CommandWriteByte);

        ctrl.Read(3).Should().Be(0x01, "status should be NACK");
    }
}
