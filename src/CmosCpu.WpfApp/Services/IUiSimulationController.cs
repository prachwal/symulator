using CmosCpu.Core;

namespace CmosCpu.WpfApp.Services;

public interface IUiSimulationController
{
    bool IsRunning { get; }
    int SpeedHz { get; set; }

    bool LoadAsm(string path, out IReadOnlyList<string> errors);
    bool LoadBin(string path, ushort loadAddress, out string? error);

    void Reset();
    void Step();
    Task RunAsync(CancellationToken cancellationToken);
    void Pause();
    void Stop();

    SimulatorSnapshot GetSnapshot();

    event EventHandler<SimulatorSnapshot>? SnapshotChanged;
    event EventHandler<TraceEntry>? TraceEntryAdded;
    event EventHandler<BusTransaction>? BusTransactionOccurred;
}
