using CmosCpu.Core;

using NLog;

namespace CmosCpu.Devices;

public class RtcDevice : IBusDevice
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private int _second;
    private int _minute;
    private int _hour;
    private int _day;
    private int _month;
    private int _year;

    public ushort StartAddress => 0xC020;
    public ushort EndAddress => 0xC025;

    public bool Contains(ushort address)
    {
        return address >= StartAddress && address <= EndAddress;
    }

    public byte Read(ushort address)
    {
        return address switch
        {
            0xC020 => (byte)_second,
            0xC021 => (byte)_minute,
            0xC022 => (byte)_hour,
            0xC023 => (byte)_day,
            0xC024 => (byte)_month,
            0xC025 => (byte)_year,
            _ => 0,
        };
    }

    public void Write(ushort address, byte value)
    {
        switch (address)
        {
            case 0xC020: _second = value; break;
            case 0xC021: _minute = value; break;
            case 0xC022: _hour = value; break;
            case 0xC023: _day = value; break;
            case 0xC024: _month = value; break;
            case 0xC025: _year = value; break;
        }
        Logger.Debug("RTC set: {Year}-{Month}-{Day} {Hour}:{Minute}:{Second}", _year, _month, _day, _hour, _minute, _second);
    }
}