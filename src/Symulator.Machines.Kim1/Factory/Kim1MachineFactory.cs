using CmosCpu.Computer;
using CmosCpu.Cpu;
using CmosCpu.Core;
using NLog;
using Symulator.Application.Abstractions;
using Symulator.Machines.Kim1.Devices;

namespace Symulator.Machines.Kim1.Factory;

public static class Kim1MachineFactory
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Kim1MachineRuntime Create(string basePath)
    {
        Logger.Debug("Kim1MachineFactory.Create called with basePath={BasePath}", basePath);
        var profilePath = Path.Combine(basePath, "profiles", "kim-1.json");
        if (!File.Exists(profilePath))
        {
            Logger.Debug("KIM-1 profile not found at {ProfilePath}; searching upwards", profilePath);
            profilePath = FindProfileUpwards(basePath, "kim-1.json");
        }

        Logger.Info("Using KIM-1 profile: {ProfilePath}", profilePath);

        var profile = ComputerProfileLoader.LoadFromFile(profilePath);
        if (!profile.IsValid || profile.Profile is null)
            throw new InvalidOperationException($"Invalid KIM-1 profile: {string.Join("; ", profile.Errors)}");

        var machine = new ComputerMachine(profile.Profile, mapDevicesFromProfile: false);
        machine.Reset();

        var romRiot = CreateRiot(profile.Profile, "kim1-6530-io", 0x1700);
        var ioRiot = CreateRiot(profile.Profile, "kim1-6530-003-io", 0x1400);
        var keypad = new Kim1KeypadState();
        var ledDisplay = new Kim1LedDisplayState();

        var devices = new Kim1MachineDevices(romRiot, ioRiot, keypad, ledDisplay);
        devices.AttachTo(machine);

        LoadRomFiles(machine, Path.GetDirectoryName(profilePath)!);

        Logger.Debug("KIM-1 ComputerMachine created successfully");
        return new Kim1MachineRuntime(machine, devices);
    }

    private static Kim1Riot6530IoDevice CreateRiot(ComputerProfile profile, string deviceType, ushort defaultStart)
    {
        var dev = profile.Devices?.FirstOrDefault(d => d.Type == deviceType);
        var start = dev is not null && !string.IsNullOrEmpty(dev.Start)
            ? ComputerProfileLoader.ParseHex(dev.Start)
            : defaultStart;
        var size = dev is not null && !string.IsNullOrEmpty(dev.Size)
            ? ComputerProfileLoader.ParseHex(dev.Size)
            : (ushort)0x0100;
        return new Kim1Riot6530IoDevice(start, (ushort)(start + size - 1));
    }

    private static void LoadRomFiles(ComputerMachine machine, string profileDir)
    {
        var romSections = machine.Profile.Memory?.Rom;
        if (romSections is null) return;

        var solutionRoot = FindSolutionRoot(profileDir);
        if (solutionRoot is null) return;

        foreach (var rom in romSections)
        {
            if (string.IsNullOrEmpty(rom.File)) continue;

            var romPath = Path.Combine(solutionRoot, rom.File);
            if (!File.Exists(romPath))
            {
                Logger.Warn("KIM-1 ROM file not found: {RomPath}", romPath);
                continue;
            }

            var data = File.ReadAllBytes(romPath);
            var start = ComputerProfileLoader.ParseHex(rom.Start!);
            for (int i = 0; i < data.Length && i < (int)ComputerProfileLoader.ParseHex(rom.Size!); i++)
                machine.Memory.WriteByte((ushort)(start + i), data[i]);
            Logger.Info("Loaded KIM-1 ROM: {File} at 0x{Start:X4} ({Size} bytes)", rom.File, start, data.Length);
        }
    }

    private static string FindProfileUpwards(string startDir, string profileName)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "profiles", profileName);
            Logger.Debug("Checking KIM-1 profile candidate: {Candidate}", candidate);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        var solutionRoot = FindSolutionRoot(startDir);
        if (solutionRoot is not null)
        {
            var slnCandidate = Path.Combine(solutionRoot, "profiles", profileName);
            Logger.Debug("Checking KIM-1 solution-root profile candidate: {Candidate}", slnCandidate);
            if (File.Exists(slnCandidate))
                return slnCandidate;
        }

        throw new FileNotFoundException($"Cannot find profile: {profileName}. Looked from {startDir}");
    }

    public static string? FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            if (dir.GetFiles("*.slnx").Length > 0 || dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    public static CpuStateSnapshot BuildCpuSnapshot(Mos6502Cpu cpu)
    {
        var flags = $"{(cpu.Negative ? 'N' : '-')}{(cpu.Zero ? 'Z' : '-')}{(cpu.Carry ? 'C' : '-')}{(cpu.InterruptDisable ? 'I' : '-')}";

        return new CpuStateSnapshot(
            $"${cpu.PC:X4}",
            $"${cpu.A:X2}",
            $"${cpu.X:X2}",
            $"${cpu.Y:X2}",
            $"${cpu.SP:X2}",
            flags,
            cpu.CycleCount,
            cpu.IsHalted,
            Array.Empty<CpuRegisterSnapshot>()
        );
    }

    public static Kim1RiotSnapshot BuildRiotSnapshot(Kim1MachineRuntime runtime)
    {
        var riot = runtime.Devices.Riot002;
        return new Kim1RiotSnapshot
        {
            PortA = riot.PortAData.ToString("X2"),
            PortB = riot.PortBData.ToString("X2"),
            DDRA = riot.PortADdr.ToString("X2"),
            DDRB = riot.PortBDdr.ToString("X2"),
        };
    }
}

public sealed class Kim1RiotSnapshot
{
    public string PortA { get; set; } = "--";
    public string PortB { get; set; } = "--";
    public string DDRA { get; set; } = "--";
    public string DDRB { get; set; } = "--";
}
