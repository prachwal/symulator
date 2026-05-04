namespace CmosCpu.Computer.Devices.Rtc;

public sealed record RtcSnapshot(
    RtcTimeMode TimeMode,
    DateTime CurrentTime,
    byte Control,
    byte Status,
    byte[] Registers,
    byte? I2cAddress = null,
    ushort? DirectBusBaseAddress = null,
    RtcBusMode? DirectBusMode = null);
