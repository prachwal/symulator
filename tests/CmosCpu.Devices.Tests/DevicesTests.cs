using CmosCpu.Devices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CmosCpu.Devices.Tests;

[TestClass]
public class DevicesTests
{
    [TestMethod]
    public void LedDevice_WriteZero_LedOff()
    {
        var led = new LedDevice();
        led.Write(0xC000, 0x00);
        led.IsOn.Should().BeFalse();
        led.Read(0xC000).Should().Be(0);
    }

    [TestMethod]
    public void LedDevice_WriteNonZero_LedOn()
    {
        var led = new LedDevice();
        led.Write(0xC000, 0x01);
        led.IsOn.Should().BeTrue();
        led.Read(0xC000).Should().Be(1);
    }

    [TestMethod]
    public void LedDevice_StateChangedEvent_FiresOnChange()
    {
        var led = new LedDevice();
        bool eventFired = false;
        led.StateChanged += (s, e) => eventFired = true;
        led.Write(0xC000, 0x01);
        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void LedDevice_StateChangedEvent_DoesNotFireOnSameState()
    {
        var led = new LedDevice();
        int fireCount = 0;
        led.StateChanged += (s, e) => fireCount++;
        led.Write(0xC000, 0x01);
        led.Write(0xC000, 0x01);
        fireCount.Should().Be(1);
    }

    [TestMethod]
    public void LedDevice_Contains_ReturnsTrueForBaseAddress()
    {
        var led = new LedDevice();
        led.Contains(0xC000).Should().BeTrue();
        led.Contains(0xC001).Should().BeFalse();
    }

    [TestMethod]
    public void LedDevice_AnyNonZeroValue_TurnsOn()
    {
        var led = new LedDevice();
        led.Write(0xC000, 0xFF);
        led.IsOn.Should().BeTrue();
    }

    [TestMethod]
    public void TimerDevice_WriteCounter_UpdatesReloadValue()
    {
        var timer = new TimerDevice();
        timer.Write(0xC010, 0x42);
        timer.Read(0xC010).Should().Be(0x42);
    }

    [TestMethod]
    public void TimerDevice_Tick_DecrementsCounter()
    {
        var timer = new TimerDevice();
        timer.Write(0xC010, 0x05);
        timer.Write(0xC011, 0x80);
        timer.Tick();
        timer.Tick();
        timer.Tick();
        timer.Read(0xC010).Should().Be(0x02);
    }

    [TestMethod]
    public void TimerDevice_Tick_FiresWhenZero()
    {
        var timer = new TimerDevice();
        bool fired = false;
        timer.TimerFired += (s, e) => fired = true;
        timer.Write(0xC010, 0x01);
        timer.Write(0xC011, 0x80);
        timer.Tick();
        fired.Should().BeTrue();
        timer.Read(0xC012).Should().Be(1);
    }

    [TestMethod]
    public void TimerDevice_NotRunning_DoesNotTick()
    {
        var timer = new TimerDevice();
        timer.Write(0xC010, 0x05);
        timer.Tick();
        timer.Read(0xC010).Should().Be(5);
    }

    [TestMethod]
    public void RtcDevice_WriteThenRead_ReturnsValues()
    {
        var rtc = new RtcDevice();
        rtc.Write(0xC020, 30);
        rtc.Write(0xC021, 15);
        rtc.Write(0xC022, 12);
        rtc.Read(0xC020).Should().Be(30);
        rtc.Read(0xC021).Should().Be(15);
        rtc.Read(0xC022).Should().Be(12);
    }

    [TestMethod]
    public void RtcDevice_Contains_ChecksAddressRange()
    {
        var rtc = new RtcDevice();
        rtc.Contains(0xC020).Should().BeTrue();
        rtc.Contains(0xC025).Should().BeTrue();
        rtc.Contains(0xC01F).Should().BeFalse();
        rtc.Contains(0xC026).Should().BeFalse();
    }
}
