namespace CmosCpu.Computer.Devices.Rtc;

public sealed class RtcClockCore
{
    public const byte SecondsRegister = 0x00;
    public const byte MinutesRegister = 0x01;
    public const byte HoursRegister = 0x02;
    public const byte DayRegister = 0x03;
    public const byte DateRegister = 0x04;
    public const byte MonthRegister = 0x05;
    public const byte YearRegister = 0x06;
    public const byte ControlRegister = 0x0E;
    public const byte StatusRegister = 0x0F;
    public const int RegisterCount = 0x40;

    private readonly Func<DateTime> _hostClock;
    private readonly byte[] _nvram = new byte[RegisterCount];
    private DateTime _currentTime;

    public RtcClockCore(
        RtcTimeMode timeMode = RtcTimeMode.Manual,
        DateTime? initialTime = null,
        Func<DateTime>? hostClock = null)
    {
        TimeMode = timeMode;
        _currentTime = Normalize(initialTime ?? new DateTime(1977, 4, 11, 0, 0, 0, DateTimeKind.Unspecified));
        _hostClock = hostClock ?? (() => DateTime.Now);
    }

    public RtcTimeMode TimeMode { get; set; }

    public byte Control { get; private set; }

    public byte Status { get; private set; }

    public DateTime CurrentTime
    {
        get => TimeMode == RtcTimeMode.Host ? Normalize(_hostClock()) : _currentTime;
        set => _currentTime = Normalize(value);
    }

    public void Tick(TimeSpan elapsed)
    {
        if (TimeMode == RtcTimeMode.Simulated)
            _currentTime = Normalize(_currentTime.Add(elapsed));
    }

    public byte ReadRegister(byte register)
    {
        var time = CurrentTime;
        return register switch
        {
            SecondsRegister => ToBcd(time.Second),
            MinutesRegister => ToBcd(time.Minute),
            HoursRegister => ToBcd(time.Hour),
            DayRegister => ToBcd(ToDsDayOfWeek(time.DayOfWeek)),
            DateRegister => ToBcd(time.Day),
            MonthRegister => ToBcd(time.Month),
            YearRegister => ToBcd(time.Year % 100),
            ControlRegister => Control,
            StatusRegister => Status,
            >= 0x20 and < RegisterCount => _nvram[register],
            _ => 0x00
        };
    }

    public void WriteRegister(byte register, byte value)
    {
        switch (register)
        {
            case SecondsRegister:
                UpdateTime(value, 0, 59, (t, v) => new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, v, t.Kind));
                break;
            case MinutesRegister:
                UpdateTime(value, 0, 59, (t, v) => new DateTime(t.Year, t.Month, t.Day, t.Hour, v, t.Second, t.Kind));
                break;
            case HoursRegister:
                UpdateTime(value, 0, 23, (t, v) => new DateTime(t.Year, t.Month, t.Day, v, t.Minute, t.Second, t.Kind));
                break;
            case DayRegister:
                // DS3231 day-of-week is informational here; DateTime computes it from date.
                _ = TryFromBcd(value, 1, 7, out _);
                break;
            case DateRegister:
                UpdateDatePart(value, 1, 31, (t, v) => TryCreate(t.Year, t.Month, v, t.Hour, t.Minute, t.Second, t.Kind, out var next) ? next : t);
                break;
            case MonthRegister:
                UpdateDatePart(value, 1, 12, (t, v) => TryCreate(t.Year, v, Math.Min(t.Day, DateTime.DaysInMonth(t.Year, v)), t.Hour, t.Minute, t.Second, t.Kind, out var next) ? next : t);
                break;
            case YearRegister:
                UpdateDatePart(value, 0, 99, (t, v) =>
                {
                    var year = 2000 + v;
                    var day = Math.Min(t.Day, DateTime.DaysInMonth(year, t.Month));
                    return TryCreate(year, t.Month, day, t.Hour, t.Minute, t.Second, t.Kind, out var next) ? next : t;
                });
                break;
            case ControlRegister:
                Control = value;
                break;
            case StatusRegister:
                Status = value;
                break;
            case >= 0x20 and < RegisterCount:
                _nvram[register] = value;
                break;
        }
    }

    public RtcSnapshot CreateSnapshot(byte? i2cAddress = null, ushort? directBusBaseAddress = null, RtcBusMode? directBusMode = null)
    {
        var registers = new byte[0x10];
        for (byte i = 0; i < registers.Length; i++)
            registers[i] = ReadRegister(i);

        return new RtcSnapshot(TimeMode, CurrentTime, Control, Status, registers, i2cAddress, directBusBaseAddress, directBusMode);
    }

    public static byte ToBcd(int value)
    {
        if (value < 0 || value > 99)
            throw new ArgumentOutOfRangeException(nameof(value), value, "BCD value must be in range 0..99.");

        return (byte)(((value / 10) << 4) | (value % 10));
    }

    public static bool TryFromBcd(byte value, int min, int max, out int decoded)
    {
        var high = (value >> 4) & 0x0F;
        var low = value & 0x0F;
        decoded = high * 10 + low;
        return high <= 9 && low <= 9 && decoded >= min && decoded <= max;
    }

    private void UpdateTime(byte bcdValue, int min, int max, Func<DateTime, int, DateTime> update)
    {
        if (!TryFromBcd(bcdValue, min, max, out var decoded))
            return;

        _currentTime = Normalize(update(CurrentTime, decoded));
    }

    private void UpdateDatePart(byte bcdValue, int min, int max, Func<DateTime, int, DateTime> update)
    {
        if (!TryFromBcd(bcdValue, min, max, out var decoded))
            return;

        _currentTime = Normalize(update(CurrentTime, decoded));
    }

    private static bool TryCreate(int year, int month, int day, int hour, int minute, int second, DateTimeKind kind, out DateTime value)
    {
        try
        {
            value = new DateTime(year, month, day, hour, minute, second, kind);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            value = default;
            return false;
        }
    }

    private static DateTime Normalize(DateTime value) => new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind);

    private static int ToDsDayOfWeek(DayOfWeek dayOfWeek) => dayOfWeek == DayOfWeek.Sunday ? 1 : (int)dayOfWeek + 1;
}
