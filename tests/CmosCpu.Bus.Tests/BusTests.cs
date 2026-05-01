using CmosCpu.Bus;
using CmosCpu.Core;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CmosCpu.Bus.Tests;

[TestClass]
public class BusTests
{
    [TestMethod]
    public void Read_UnmappedAddress_ReturnsZero()
    {
        var bus = new SystemBus();
        byte value = bus.Read(0x1234);
        value.Should().Be(0);
    }

    [TestMethod]
    public void Write_UnmappedAddress_DoesNotThrow()
    {
        var bus = new SystemBus();
        bus.Invoking(b => b.Write(0x1234, 0xFF)).Should().NotThrow();
    }

    [TestMethod]
    public void Read_MapsToCorrectDevice()
    {
        var bus = new SystemBus();
        var device = new Mock<IBusDevice>();
        device.Setup(d => d.Contains(0x1000)).Returns(true);
        device.Setup(d => d.Read(0x1000)).Returns(0x42);
        bus.AttachDevice(device.Object);

        byte value = bus.Read(0x1000);
        value.Should().Be(0x42);
    }

    [TestMethod]
    public void Write_MapsToCorrectDevice()
    {
        var bus = new SystemBus();
        var device = new Mock<IBusDevice>();
        device.Setup(d => d.Contains(0x2000)).Returns(true);
        bus.AttachDevice(device.Object);

        bus.Write(0x2000, 0xAB);
        device.Verify(d => d.Write(0x2000, 0xAB), Times.Once);
    }

    [TestMethod]
    public void AttachDevice_DeviceReceivesReads()
    {
        var bus = new SystemBus();
        var device = new Mock<IBusDevice>();
        device.Setup(d => d.Contains(It.IsAny<ushort>())).Returns(true);
        device.Setup(d => d.Read(0x3000)).Returns(0x77);
        bus.AttachDevice(device.Object);

        bus.Read(0x3000).Should().Be(0x77);
    }

    [TestMethod]
    public void DetachDevice_NoLongerReceivesReads()
    {
        var bus = new SystemBus();
        var device = new Mock<IBusDevice>();
        device.Setup(d => d.Contains(It.IsAny<ushort>())).Returns(true);
        bus.AttachDevice(device.Object);
        bus.DetachDevice(device.Object);

        bus.Read(0x3000).Should().Be(0);
    }

    [TestMethod]
    public void ClearDevices_RemovesAllDevices()
    {
        var bus = new SystemBus();
        var device = new Mock<IBusDevice>();
        device.Setup(d => d.Contains(It.IsAny<ushort>())).Returns(true);
        bus.AttachDevice(device.Object);
        bus.ClearDevices();

        bus.Read(0x3000).Should().Be(0);
    }
}
