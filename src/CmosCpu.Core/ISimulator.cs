namespace CmosCpu.Core;

public interface ISimulator
{
    void Reset();
    void Step();
    void Run(int maxCycles);
    SimulatorSnapshot GetSnapshot();
}
