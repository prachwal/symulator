using CmosCpu.Terminal.Session;

namespace CmosCpu.Terminal.Tui;

public sealed class TerminalScreenFactory : ITerminalScreenFactory
{
    private static readonly Dictionary<string, Func<string[], ITerminalScreen>> Creators = new(StringComparer.OrdinalIgnoreCase)
    {
        ["apple-1"] = CreateApple1,
        ["kim-1"] = CreateKim1,
    };

    public ITerminalScreen Create(string platformId)
    {
        if (Creators.TryGetValue(platformId, out var factory))
            return factory([]);
        throw new ArgumentException($"Unknown platform: {platformId}", nameof(platformId));
    }

    public ITerminalScreen Create(string platformId, string[] args)
    {
        if (Creators.TryGetValue(platformId, out var factory))
            return factory(args);
        throw new ArgumentException($"Unknown platform: {platformId}", nameof(platformId));
    }

    public bool Supports(string platformId) => Creators.ContainsKey(platformId);

    private static ITerminalScreen CreateApple1(string[] args)
    {
        bool traceState = args is not null &&
                          GetArgValue(args, "--trace-state") is not null &&
                          bool.TryParse(GetArgValue(args, "--trace-state"), out var ts) && ts;
        return new Apple1TuiScreen { TraceState = traceState };
    }

    private static ITerminalScreen CreateKim1(string[] args)
    {
        return new Kim1TuiScreen();
    }

    public static bool LoadProfileForTui(ISimulatorSession session, string profilePath)
    {
        if (session.IsLoaded)
            return true;

        if (File.Exists(profilePath))
            return session.LoadProfileFromFile(profilePath);

        return false;
    }

    private static string? GetArgValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}
