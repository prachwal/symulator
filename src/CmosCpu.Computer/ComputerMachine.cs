using CmosCpu.Computer.Abstractions;
using CmosCpu.Core;
using CmosCpu.Cpu;

namespace CmosCpu.Computer;

public sealed class ComputerMachine
{
    private readonly List<IClockedDevice> _clockedDevices = [];
    private readonly List<IInterruptSource> _interruptSources = [];
    private readonly List<IMachineStepHook> _stepHooks = [];
    private readonly bool _mapDevicesFromProfile;

    public ComputerProfile Profile { get; }
    public Mos6502Cpu Cpu { get; }
    public ComputerMemoryBus Memory { get; }
    public TextDisplayRegion? TextDisplay { get; private set; }
    public KeyboardRegion? Keyboard { get; private set; }
    public bool IsRunning { get; private set; }

    public ComputerMachine(ComputerProfile profile, bool mapDevicesFromProfile = true)
    {
        Profile = profile;
        _mapDevicesFromProfile = mapDevicesFromProfile;
        Memory = new ComputerMemoryBus();
        BuildMemoryMap();
        Cpu = new Mos6502Cpu(Memory);
    }

    public void Reset()
    {
        Cpu.Reset();
        IsRunning = false;
    }

    public int Step()
    {
        bool irqPending = _interruptSources.Any(s => s.IrqPending);
        Cpu.SetIrqLine(irqPending);

        int cycles = Cpu.Step();

        if (cycles > 0)
        {
            ulong uCycles = (ulong)cycles;
            foreach (var device in _clockedDevices)
                device.Tick(uCycles);

            foreach (var hook in _stepHooks)
                hook.AfterStep(cycles);
        }

        return cycles;
    }

    public void RunSteps(int maxSteps)
    {
        IsRunning = true;
        try
        {
            for (int i = 0; i < maxSteps && IsRunning; i++)
            {
                if (Cpu.IsHalted)
                    break;
                Step();
            }
        }
        finally
        {
            IsRunning = false;
        }
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void MapDevice(IMemoryMappedDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        var size = checked((ushort)(device.EndAddress - device.StartAddress + 1));
        Memory.MapDevice(device.StartAddress, size, device.Read, device.Write);

        if (device is IClockedDevice clocked)
            AddClockedDevice(clocked);

        if (device is IInterruptSource interruptSource)
            AddInterruptSource(interruptSource);
    }

    public void MapDevice(ushort start, ushort size, Func<ushort, byte> read, Action<ushort, byte> write)
    {
        Memory.MapDevice(start, size, read, write);
    }

    public void AddClockedDevice(IClockedDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!_clockedDevices.Contains(device))
            _clockedDevices.Add(device);
    }

    public void AddInterruptSource(IInterruptSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!_interruptSources.Contains(source))
            _interruptSources.Add(source);
    }

    public void AddStepHook(IMachineStepHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        if (!_stepHooks.Contains(hook))
            _stepHooks.Add(hook);
    }

    private void BuildMemoryMap()
    {
        var mem = Profile.Memory;
        if (mem is null) return;

        if (mem.Ram is not null)
            foreach (var section in mem.Ram)
            {
                var start = ComputerProfileLoader.ParseHex(section.Start!);
                var size = ComputerProfileLoader.ParseHex(section.Size!);
                Memory.MapRam(start, size);
            }

        if (mem.Rom is not null)
            foreach (var section in mem.Rom)
            {
                var start = ComputerProfileLoader.ParseHex(section.Start!);
                var size = ComputerProfileLoader.ParseHex(section.Size!);
                Memory.MapRom(start, size);

                if (section.Mirrors is not null)
                    foreach (var mirror in section.Mirrors)
                    {
                        var mirrorStart = ComputerProfileLoader.ParseHex(mirror.Start!);
                        var mirrorSize = string.IsNullOrEmpty(mirror.Size)
                            ? size
                            : ComputerProfileLoader.ParseHex(mirror.Size!);
                        Memory.MapRomMirror(start, mirrorStart, mirrorSize);
                    }
            }

        if (_mapDevicesFromProfile && Profile.Devices is not null)
            foreach (var dev in Profile.Devices)
                MapDeviceFromProfile(dev);
    }

    private void MapDeviceFromProfile(ComputerProfileDevice dev)
    {
        switch (dev.Type)
        {
            case "text-display":
            {
                var start = ComputerProfileLoader.ParseHex(dev.Start!);
                int width = dev.Width > 0 ? dev.Width : 40;
                int height = dev.Height > 0 ? dev.Height : 25;
                var display = new TextDisplayRegion(start, width, height);
                TextDisplay = display;
                int size = width * height;
                Memory.MapDevice(start, (ushort)size, display.Read, display.Write);
                break;
            }
            case "keyboard":
            {
                if (string.IsNullOrWhiteSpace(dev.DataAddress) || string.IsNullOrWhiteSpace(dev.StatusAddress))
                    break;
                var dataAddr = ComputerProfileLoader.ParseHex(dev.DataAddress!);
                var statusAddr = ComputerProfileLoader.ParseHex(dev.StatusAddress!);
                var keyboard = new KeyboardRegion(dataAddr, statusAddr);
                Keyboard = keyboard;
                ushort start = dataAddr < statusAddr ? dataAddr : statusAddr;
                ushort end = dataAddr > statusAddr ? dataAddr : statusAddr;
                int size = end - start + 1;
                Memory.MapDevice(start, (ushort)size, addr =>
                {
                    if (addr == keyboard.DataAddress) return keyboard.ReadData();
                    if (addr == keyboard.StatusAddress) return keyboard.ReadStatus();
                    return 0;
                }, (addr, val) => { });
                break;
            }
            case "character-rom":
            case "text-terminal":
                break;
            default:
                break;
        }
    }
}
