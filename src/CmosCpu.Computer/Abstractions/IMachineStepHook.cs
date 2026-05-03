namespace CmosCpu.Computer.Abstractions;

public interface IMachineStepHook
{
    void AfterStep(int cpuCycles);
}
