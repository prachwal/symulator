using CmosCpu.Computer;
using CmosCpu.Core;
using CmosCpu.Cpu;

namespace Symulator.Machines.Retro70;

public sealed class Retro70Service
{
    private ComputerMachine? _machine;

    private const string DefaultProfileJson = """
    {
        "id": "retro70-mos6502",
        "name": "Retro70 MOS 6502 Computer",
        "cpu": "mos6502",
        "clockHz": 1000000,
        "memory": {
            "ram": [{ "start": "0x0000", "size": "0x8000" }],
            "rom": [{ "start": "0xF000", "size": "0x1000" }],
            "vectors": { "reset": "0xF000", "nmi": "0xF100", "irq": "0xF200" }
        },
        "devices": [
            { "type": "text-display", "id": "screen", "start": "0xD000", "width": 40, "height": 25 },
            { "type": "keyboard", "id": "keyboard", "dataAddress": "0xD800", "statusAddress": "0xD801" }
        ]
    }
    """;

    public ComputerMachine? Machine => _machine;
    public Mos6502Cpu? Cpu => _machine?.Cpu;
    public ComputerProfile? Profile => _machine?.Profile;
    public ComputerMemoryBus? Memory => _machine?.Memory;
    public TextDisplayRegion? TextDisplay => _machine?.TextDisplay;
    public KeyboardRegion? Keyboard => _machine?.Keyboard;
    public bool IsLoaded => _machine is not null;

    public string StatusMessage
    {
        get
        {
            if (_machine is null) return "No profile loaded";
            if (_machine.Cpu.IsHalted) return "HALTED";
            return _machine.IsRunning ? "Running..." : "Stopped";
        }
    }

    public event Action? StateChanged;

    public bool LoadProfile(string json)
    {
        var result = ComputerProfileLoader.Load(json);
        if (!result.IsValid || result.Profile is null)
            return false;

        _machine = new ComputerMachine(result.Profile);
        NotifyStateChanged();
        return true;
    }

    public string DefaultProfileJsonString() => DefaultProfileJson;

    public bool LoadDefaultProfile()
    {
        var result = ComputerProfileLoader.Load(DefaultProfileJson);
        if (!result.IsValid || result.Profile is null)
            return false;

        _machine = new ComputerMachine(result.Profile);
        NotifyStateChanged();
        return true;
    }

    public bool LoadMonitorRom()
    {
        if (_machine is null) return false;

        var rom = Retro70MonitorRomBuilder.Build();
        _machine.Memory.LoadRom(0xF000, rom);
        NotifyStateChanged();
        return true;
    }

    public void Reset()
    {
        _machine?.Reset();
        NotifyStateChanged();
    }

    public void Step()
    {
        _machine?.Step();
        NotifyStateChanged();
    }

    public void RunSteps(int steps)
    {
        _machine?.RunSteps(steps);
        NotifyStateChanged();
    }

    public void Stop()
    {
        _machine?.Stop();
        NotifyStateChanged();
    }

    public void ClearScreen()
    {
        _machine?.TextDisplay?.Clear();
        NotifyStateChanged();
    }

    public bool LoadBinary(byte[] data, ushort startAddress, bool setResetVector = true)
    {
        if (_machine is null) return false;

        for (int i = 0; i < data.Length; i++)
        {
            _machine.Memory.WriteByte((ushort)(startAddress + i), data[i]);
        }

        if (setResetVector)
        {
            _machine.Memory.WriteByte(0xFFFC, (byte)(startAddress & 0xFF));
            _machine.Memory.WriteByte(0xFFFD, (byte)((startAddress >> 8) & 0xFF));
        }

        NotifyStateChanged();
        return true;
    }

    public void EnqueueKey(char c)
    {
        if (_machine?.Keyboard is not null)
        {
            _machine.Keyboard.EnqueueKey(c);
        }
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
