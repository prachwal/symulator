namespace Symulator.Machines.Apple1.Models;

public enum Apple1BootState
{
    NotInitialized,
    Reset,
    BootingMonitor,
    MonitorReady,
    BootingBasic,
    BasicReady,
    Running,
    Paused,
    Halted,
    Error
}
