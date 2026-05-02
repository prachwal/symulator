namespace CmosCpu.Core;

public interface IMachineBuilder
{
    IBus Bus { get; }
    IMachineBuilder WithCpu(ICpuCore cpu);
    IMachineBuilder WithDevice(IBusDevice device);
    IMachineBuilder WithClockedDevice(IClockedDevice device);
    IMachineBuilder WithDebugger(IDebugger debugger);
    IMachine Build();
}