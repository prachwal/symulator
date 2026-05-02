using CmosCpu.Computer;
using CmosCpu.Cpu;
using CmosCpu.Core;
using Symulator.Application.Abstractions;

namespace Symulator.Machines.Kim1.Factory;

public static class Kim1MachineFactory
{
    public static ComputerMachine Create(string basePath)
    {
        var profilePath = Path.Combine(basePath, "profiles", "kim-1.json");
        if (!File.Exists(profilePath))
        {
            profilePath = FindProfileUpwards(basePath, "kim-1.json");
        }

        var machine = ComputerMachineFactory.CreateFromFile(profilePath);
        return machine;
    }

    private static string FindProfileUpwards(string startDir, string profileName)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "profiles", profileName);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        var solutionRoot = FindSolutionRoot(startDir);
        if (solutionRoot is not null)
        {
            var slnCandidate = Path.Combine(solutionRoot, "profiles", profileName);
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

    public static Kim1RiotSnapshot BuildRiotSnapshot(ComputerMachine machine)
    {
        return new Kim1RiotSnapshot
        {
            PortA = machine.Kim1Riot?.PortAData.ToString("X2") ?? "--",
            PortB = machine.Kim1Riot?.PortBData.ToString("X2") ?? "--",
            DDRA = machine.Kim1Riot?.PortADdr.ToString("X2") ?? "--",
            DDRB = machine.Kim1Riot?.PortBDdr.ToString("X2") ?? "--",
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
