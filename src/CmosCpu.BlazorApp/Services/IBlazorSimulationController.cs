using CmosCpu.BlazorApp.Models;
using CmosCpu.Core;

namespace CmosCpu.BlazorApp.Services;

public interface IBlazorSimulationController
{
    bool IsRunning { get; }
    int SpeedHz { get; set; }

    Task LoadAsmAsync(Stream stream, string fileName, CancellationToken cancellationToken);
    Task LoadBinAsync(Stream stream, string fileName, ushort loadAddress, CancellationToken cancellationToken);

    CompileResultViewModel CompileAsm(string source);
    CompileResultViewModel CompileLoadResetAsm(string source);

    void Reset();
    void Step();
    Task RunAsync(CancellationToken cancellationToken);
    void Pause();
    void Stop();

    void ReportStatus(string message);

    SimulatorSnapshot GetSnapshot();

    event EventHandler<SimulatorSnapshot>? SnapshotChanged;
    event EventHandler<TraceEntry>? TraceEntryAdded;
    event EventHandler<BusTransaction>? BusTransactionAdded;
    event EventHandler<string>? StatusChanged;
}
