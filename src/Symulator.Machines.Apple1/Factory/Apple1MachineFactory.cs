using CmosCpu.Computer;
using CmosCpu.Cpu;
using CmosCpu.Core;
using NLog;
using Symulator.Application.Abstractions;

namespace Symulator.Machines.Apple1.Factory;

public static class Apple1MachineFactory
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static ComputerMachine Create(string basePath)
    {
        Logger.Debug("Apple1MachineFactory.Create called with basePath={BasePath}", basePath);
        var profilePath = Path.Combine(basePath, "profiles", "apple-1.json");
        if (!File.Exists(profilePath))
        {
            Logger.Debug("Apple-1 profile not found at {ProfilePath}; searching upwards", profilePath);
            profilePath = FindProfileUpwards(basePath, "apple-1.json");
        }

        Logger.Info("Using Apple-1 profile: {ProfilePath}", profilePath);

        var machine = ComputerMachineFactory.CreateFromFile(profilePath);

        var solutionRoot = FindSolutionRoot(Path.GetDirectoryName(profilePath)!);
        if (solutionRoot is not null)
        {
            Logger.Debug("Apple-1 solution root: {SolutionRoot}", solutionRoot);
            LoadRomFiles(machine, solutionRoot);
        }
        else
        {
            Logger.Warn("Could not determine solution root; ROM files will not be loaded");
        }

        LogDiagnostics(machine);

        Logger.Debug("Apple-1 ComputerMachine created successfully");
        return machine;
    }

    private static void LoadRomFiles(ComputerMachine machine, string baseDir)
    {
        var romSections = machine.Profile.Memory?.Rom;
        if (romSections is null) return;

        foreach (var rom in romSections)
        {
            if (string.IsNullOrEmpty(rom.File)) continue;

            var romPath = Path.Combine(baseDir, rom.File);
            if (!File.Exists(romPath))
            {
                Logger.Warn("Apple-1 ROM file not found: {RomPath}", romPath);
                continue;
            }

            var data = File.ReadAllBytes(romPath);
            var start = ComputerProfileLoader.ParseHex(rom.Start!);
            machine.Memory.LoadRom(start, data);
            Logger.Info("Loaded Apple-1 ROM: {File} at 0x{Start:X4} ({Size} bytes)", rom.File, start, data.Length);
        }
    }

    private static void LogDiagnostics(ComputerMachine machine)
    {
        byte resetLo = machine.Memory.ReadByte(0xFFFC);
        byte resetHi = machine.Memory.ReadByte(0xFFFD);
        ushort resetVector = (ushort)((resetHi << 8) | resetLo);
        Logger.Debug("Apple-1 reset vector at $FFFC: 0x{ResetLo:X2}:0x{ResetHi:X2} -> 0x{ResetVector:X4}", resetLo, resetHi, resetVector);
        Logger.Debug("Apple-1 PC after machine construction: 0x{PC:X4}", machine.Cpu.PC);
    }

    private static string FindProfileUpwards(string startDir, string profileName)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "profiles", profileName);
            Logger.Debug("Checking Apple-1 profile candidate: {Candidate}", candidate);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        var solutionRoot = FindSolutionRoot(startDir);
        if (solutionRoot is not null)
        {
            var slnCandidate = Path.Combine(solutionRoot, "profiles", profileName);
            Logger.Debug("Checking Apple-1 solution-root profile candidate: {Candidate}", slnCandidate);
            if (File.Exists(slnCandidate))
                return slnCandidate;
        }

        throw new FileNotFoundException($"Cannot find profile: {profileName}. Looked from {startDir}");
    }

    private static string? FindSolutionRoot(string startDir)
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
}
