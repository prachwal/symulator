using CmosCpu.Computer;
using CmosCpu.Cpu;
using CmosCpu.Core;
using Symulator.Application.Abstractions;

namespace Symulator.Machines.Apple1.Factory;

public static class Apple1MachineFactory
{
    public static ComputerMachine Create(string basePath)
    {
        var profilePath = Path.Combine(basePath, "profiles", "apple-1.json");
        if (!File.Exists(profilePath))
        {
            profilePath = FindProfileUpwards(basePath, "apple-1.json");
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
}
