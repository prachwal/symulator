using CmosCpu.Core;

namespace CmosCpu.Runtime;

public sealed class Machine : IMachine
{
    private readonly ICpuCore _cpu;
    private readonly IBus _bus;
    private readonly List<IClockedDevice> _clockedDevices;
    private readonly IDebugger? _debugger;
    private ulong _cycle;
    private volatile bool _stopped;

    public ICpuCore Cpu => _cpu;
    public IBus Bus => _bus;
    public ulong Cycle => _cycle;
    public bool IsRunning { get; private set; }
    public IDebugger? Debugger => _debugger;

    public Machine(ICpuCore cpu, IBus bus, IEnumerable<IClockedDevice> clockedDevices, IDebugger? debugger = null)
    {
        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _clockedDevices = clockedDevices?.ToList() ?? new List<IClockedDevice>();
        _debugger = debugger;
    }

    public void Reset()
    {
        _cpu.Reset();
        _cycle = 0;
        _stopped = false;

        foreach (var device in _clockedDevices.OfType<IResettable>())
            device.Reset();
    }

    public CpuStepResult StepInstruction()
    {
        var result = _cpu.StepInstruction();
        var cycles = Math.Max(1, result.Cycles);

        for (var i = 0; i < cycles; i++)
        {
            foreach (var device in _clockedDevices)
                device.Tick(_cycle);
            _cycle++;
        }

        return result;
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
            if (_debugger is not null && _debugger.IsBreakpoint(_cpu.Registers.PC))
            {
                _stopped = true;
                break;
            }

            StepInstruction();
        }

        IsRunning = false;
    }

    public void Stop()
    {
        _stopped = true;
        IsRunning = false;
    }
}