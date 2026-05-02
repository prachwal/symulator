using CmosCpu.Core;

namespace CmosCpu.Runtime;

public sealed class Machine : IMachine
{
    private readonly ICpuCore _cpu;
    private readonly IBus _bus;
    private readonly List<IClockedDevice> _clockedDevices;
    private ulong _cycle;
    private volatile bool _stopped;

    public ICpuCore Cpu => _cpu;
    public IBus Bus => _bus;
    public ulong Cycle => _cycle;
    public bool IsRunning { get; private set; }

    public Machine(ICpuCore cpu, IBus bus, IEnumerable<IClockedDevice> clockedDevices)
    {
        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _clockedDevices = clockedDevices?.ToList() ?? new List<IClockedDevice>();
    }

    public void Reset()
    {
        _cpu.Reset();
        _cycle = 0;
        _stopped = false;

        foreach (var device in _clockedDevices.OfType<IResettable>())
            device.Reset();
    }

    public void StepInstruction()
    {
        _cpu.StepInstruction();
    }

    public void StepCycle()
    {
        _cpu.Tick(_cycle);
        foreach (var device in _clockedDevices)
            device.Tick(_cycle);
        _cycle++;
    }

    public void Run(ulong maxCycles)
    {
        IsRunning = true;
        _stopped = false;
        ulong target = _cycle + maxCycles;

        while (!_cpu.IsHalted && _cycle < target && !_stopped)
        {
            _cpu.StepInstruction();
            foreach (var device in _clockedDevices)
                device.Tick(_cycle);
            _cycle++;
        }

        IsRunning = false;
    }

    public void Stop()
    {
        _stopped = true;
        IsRunning = false;
    }
}
