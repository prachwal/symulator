using CmosCpu.Core;

namespace CmosCpu.Computer;

public static class ComputerMachineFactory
{
    public static ComputerMachine CreateFromProfile(string json)
    {
        var result = ComputerProfileLoader.Load(json);
        if (!result.IsValid || result.Profile is null)
            throw new ArgumentException($"Invalid profile: {string.Join("; ", result.Errors)}");
        return new ComputerMachine(result.Profile);
    }

    public static ComputerMachine CreateFromFile(string path)
    {
        var result = ComputerProfileLoader.LoadFromFile(path);
        if (!result.IsValid || result.Profile is null)
            throw new ArgumentException($"Invalid profile: {string.Join("; ", result.Errors)}");
        return new ComputerMachine(result.Profile);
    }
}
