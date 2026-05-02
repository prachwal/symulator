using CmosCpu.Core;
using CmosCpu.Cpu;

namespace CmosCpu.Computer;

public sealed class ComputerMachine
{
    public ComputerProfile Profile { get; }
    public Mos6502Cpu Cpu { get; }
    public ComputerMemoryBus Memory { get; }
    public TextDisplayRegion? TextDisplay { get; private set; }
    public KeyboardRegion? Keyboard { get; private set; }
    public Kim1Riot6530IoDevice? Kim1Riot { get; private set; }
    public Kim1LedDisplayState? Kim1LedDisplay { get; private set; }
    public Kim1KeypadState? Kim1Keypad { get; private set; }
    public Apple1PiaTerminalDevice? Apple1Terminal { get; private set; }
    public bool IsRunning { get; private set; }

    public ComputerMachine(ComputerProfile profile)
    {
        Profile = profile;
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
        return Cpu.Step();
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
                Cpu.Step();
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

        if (Profile.Devices is not null)
            foreach (var dev in Profile.Devices)
                MapDevice(dev);
    }

    private void MapDevice(ComputerProfileDevice dev)
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
            case "kim1-6530-io":
            {
                var riot = new Kim1Riot6530IoDevice();
                Kim1Riot = riot;
                if (Kim1Keypad is not null)
                    riot.Keypad = Kim1Keypad;
                Memory.MapDevice(riot.StartAddress, (ushort)(riot.EndAddress - riot.StartAddress + 1),
                    riot.Read, riot.Write);
                break;
            }
            case "kim1-led-display":
            {
                Kim1LedDisplay = new Kim1LedDisplayState();
                break;
            }
            case "kim1-keypad":
            {
                Kim1Keypad = new Kim1KeypadState();
                if (Kim1Riot is not null)
                    Kim1Riot.Keypad = Kim1Keypad;
                break;
            }
            case "apple1-pia-terminal":
            {
                int columns = dev.Columns > 0 ? dev.Columns : 40;
                int rows = dev.Rows > 0 ? dev.Rows : 24;
                var terminal = new Apple1PiaTerminalDevice(columns, rows);
                Apple1Terminal = terminal;
                Memory.MapDevice(terminal.StartAddress, (ushort)(terminal.EndAddress - terminal.StartAddress + 1),
                    terminal.Read, terminal.Write);
                break;
            }
            case "character-rom":
            case "text-terminal":
                break;
            default:
                throw new InvalidOperationException($"Unknown device type: '{dev.Type}'");
        }
    }
}
