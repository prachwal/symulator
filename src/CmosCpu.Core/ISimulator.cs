namespace CmosCpu.Core;

public interface ISimulator
{
    void Reset();
    void Step();
    void Run(int maxCycles);
    void Pause();
    void Stop();
    SimulatorSnapshot GetSnapshot();

    event EventHandler<SimulatorSnapshot>? SnapshotChanged;
    event EventHandler<TraceEventArgs>? TraceExecuted;
}