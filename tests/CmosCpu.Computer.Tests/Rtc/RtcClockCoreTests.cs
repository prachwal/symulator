using CmosCpu.Computer.Devices.Rtc;
using FluentAssertions;

namespace CmosCpu.Computer.Tests.Rtc;

[TestClass]
public sealed class RtcClockCoreTests
{
    [TestMethod]
    public void ReadRegisters_ReturnsBcdEncodedTime()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(1977, 4, 11, 12, 34, 56));

        clock.ReadRegister(RtcClockCore.SecondsRegister).Should().Be(0x56);
        clock.ReadRegister(RtcClockCore.MinutesRegister).Should().Be(0x34);
        clock.ReadRegister(RtcClockCore.HoursRegister).Should().Be(0x12);
        clock.ReadRegister(RtcClockCore.DayRegister).Should().Be(0x02);
        clock.ReadRegister(RtcClockCore.DateRegister).Should().Be(0x11);
        clock.ReadRegister(RtcClockCore.MonthRegister).Should().Be(0x04);
        clock.ReadRegister(RtcClockCore.YearRegister).Should().Be(0x77);
    }

    [TestMethod]
    public void WriteRegisters_UpdatesTime()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2000, 1, 1, 0, 0, 0));

        clock.WriteRegister(RtcClockCore.YearRegister, 0x24);
        clock.WriteRegister(RtcClockCore.MonthRegister, 0x12);
        clock.WriteRegister(RtcClockCore.DateRegister, 0x31);
        clock.WriteRegister(RtcClockCore.HoursRegister, 0x23);
        clock.WriteRegister(RtcClockCore.MinutesRegister, 0x58);
        clock.WriteRegister(RtcClockCore.SecondsRegister, 0x59);

        clock.CurrentTime.Should().Be(new DateTime(2024, 12, 31, 23, 58, 59));
    }

    [TestMethod]
    public void InvalidBcdWrite_IsIgnored()
    {
        var initial = new DateTime(2024, 1, 2, 3, 4, 5);
        var clock = new RtcClockCore(RtcTimeMode.Manual, initial);

        clock.WriteRegister(RtcClockCore.SecondsRegister, 0x6A);
        clock.WriteRegister(RtcClockCore.MinutesRegister, 0x99);
        clock.WriteRegister(RtcClockCore.HoursRegister, 0x24);
        clock.WriteRegister(RtcClockCore.MonthRegister, 0x13);

        clock.CurrentTime.Should().Be(initial);
    }

    [TestMethod]
    public void SimulatedMode_TickAdvancesTime()
    {
        var clock = new RtcClockCore(RtcTimeMode.Simulated, new DateTime(2024, 1, 1, 0, 0, 0));

        clock.Tick(TimeSpan.FromSeconds(90));

        clock.CurrentTime.Should().Be(new DateTime(2024, 1, 1, 0, 1, 30));
    }

    [TestMethod]
    public void ManualMode_TickDoesNotAdvanceTime()
    {
        var initial = new DateTime(2024, 1, 1, 0, 0, 0);
        var clock = new RtcClockCore(RtcTimeMode.Manual, initial);

        clock.Tick(TimeSpan.FromHours(1));

        clock.CurrentTime.Should().Be(initial);
    }

    [TestMethod]
    public void HostMode_ReadsFromHostClock()
    {
        var hostTime = new DateTime(2026, 5, 4, 7, 8, 9);
        var clock = new RtcClockCore(RtcTimeMode.Host, hostClock: () => hostTime);

        clock.CurrentTime.Should().Be(hostTime);
        clock.ReadRegister(RtcClockCore.SecondsRegister).Should().Be(0x09);
    }

    [TestMethod]
    public void ControlAndStatus_ShouldRoundtrip()
    {
        var clock = new RtcClockCore();

        clock.WriteRegister(RtcClockCore.ControlRegister, 0xA5);
        clock.WriteRegister(RtcClockCore.StatusRegister, 0x5A);

        clock.ReadRegister(RtcClockCore.ControlRegister).Should().Be(0xA5);
        clock.ReadRegister(RtcClockCore.StatusRegister).Should().Be(0x5A);
    }

    [TestMethod]
    public void UnsupportedRegisters_AreSafe()
    {
        var clock = new RtcClockCore();

        clock.WriteRegister(0x10, 0xAA);

        clock.ReadRegister(0x10).Should().Be(0x00);
    }

    [TestMethod]
    public void Snapshot_ShouldExposeRegistersAndMetadata()
    {
        var clock = new RtcClockCore(RtcTimeMode.Manual, new DateTime(2024, 1, 2, 3, 4, 5));

        var snapshot = clock.CreateSnapshot(i2cAddress: 0x68, directBusBaseAddress: 0xD100, directBusMode: RtcBusMode.Linear);

        snapshot.TimeMode.Should().Be(RtcTimeMode.Manual);
        snapshot.CurrentTime.Should().Be(new DateTime(2024, 1, 2, 3, 4, 5));
        snapshot.I2cAddress.Should().Be(0x68);
        snapshot.DirectBusBaseAddress.Should().Be(0xD100);
        snapshot.DirectBusMode.Should().Be(RtcBusMode.Linear);
        snapshot.Registers[0].Should().Be(0x05);
    }
}
