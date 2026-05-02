using CmosCpu.Core;

namespace CmosCpu.Runtime;

public sealed class MachineBuilder : IMachineBuilder
{
    private readonly IBus _bus;
    private ICpuCore? _cpu;
    private readonly List<IBusDevice> _devices = new();
    private readonly List<IClockedDevice> _clockedDevices = new();

    public IBus Bus => _bus;

    public MachineBuilder()
        : this(new CmosCpu.Bus.SystemBus())
    {
    }

    public MachineBuilder(IBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public IMachineBuilder WithCpu(ICpuCore cpu)
    {
        if (_cpu is not null)
            throw new InvalidOperationException("CPU is already set");

        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
        return this;
    }

    public IMachineBuilder WithDevice(IBusDevice device)
    {
        if (device is null)
            throw new ArgumentNullException(nameof(device));

        _devices.Add(device);

        if (device is IClockedDevice clocked)
            _clockedDevices.Add(clocked);

        return this;
    }

    public IMachineBuilder WithClockedDevice(IClockedDevice device)
    {
        if (device is null)
            throw new ArgumentNullException(nameof(device));

        _clockedDevices.Add(device);
        return this;
    }

    public IMachine Build()
    {
        if (_cpu is null)
            throw new InvalidOperationException("CPU must be set before building");

        foreach (var device in _devices)
            _bus.AttachDevice(device);

        return new Machine(_cpu, _bus, _clockedDevices);
    }
}
