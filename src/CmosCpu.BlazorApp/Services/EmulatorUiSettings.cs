namespace CmosCpu.BlazorApp.Services;

public sealed class EmulatorUiSettings : IEmulatorUiSettings
{
    private bool _traceLogEnabled = true;
    private bool _busLogEnabled = true;
    private int _maxTraceLogEntries = 1000;
    private int _maxBusLogEntries = 1000;
    private int _uiRefreshIntervalMs = 250;

    public event EventHandler? Changed;

    public bool TraceLogEnabled
    {
        get => _traceLogEnabled;
        set => Set(ref _traceLogEnabled, value);
    }

    public bool BusLogEnabled
    {
        get => _busLogEnabled;
        set => Set(ref _busLogEnabled, value);
    }

    public int MaxTraceLogEntries
    {
        get => _maxTraceLogEntries;
        set => Set(ref _maxTraceLogEntries, Math.Clamp(value, 0, 100_000));
    }

    public int MaxBusLogEntries
    {
        get => _maxBusLogEntries;
        set => Set(ref _maxBusLogEntries, Math.Clamp(value, 0, 100_000));
    }

    public int UiRefreshIntervalMs
    {
        get => _uiRefreshIntervalMs;
        set => Set(ref _uiRefreshIntervalMs, Math.Clamp(value, 50, 5000));
    }

    private void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}