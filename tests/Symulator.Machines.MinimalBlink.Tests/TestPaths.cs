namespace Symulator.Machines.MinimalBlink.Tests;

internal static class TestPaths
{
    public static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(
                System.Reflection.Assembly.GetExecutingAssembly().Location);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "CmosCpuSimulator.slnx")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Could not find repo root (CmosCpuSimulator.slnx)");
        }
    }

    public static string ProgramsAsm => Path.Combine(RepoRoot, "programs", "asm");
}
