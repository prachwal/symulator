namespace CmosCpu.BlazorApp.Services;

public interface IEmulatorUiSettings
{
    bool TraceLogEnabled { get; set; }
    bool BusLogEnabled { get; set; }
    int MaxTraceLogEntries { get; set; }
    int MaxBusLogEntries { get; set; }
    int UiRefreshIntervalMs { get; set; }

    event EventHandler? Changed;
}