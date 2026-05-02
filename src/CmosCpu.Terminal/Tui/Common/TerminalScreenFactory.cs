using CmosCpu.Terminal.Tui.Apple1;
using CmosCpu.Terminal.Tui.Kim1;

namespace CmosCpu.Terminal.Tui.Common;

public sealed class TerminalScreenFactory : ITerminalScreenFactory
{
    private static readonly Dictionary<string, TerminalPlatformDescriptor> Platforms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["apple-1"] = new("apple-1", "profiles/apple-1.json", CreateApple1),
        ["kim-1"] = new("kim-1", "profiles/kim-1.json", CreateKim1),
    };

    public ITerminalScreen Create(string platformId)
    {
        if (Platforms.TryGetValue(platformId, out var descriptor))
            return descriptor.CreateScreen([]);
        throw new ArgumentException($"Unknown platform: {platformId}", nameof(platformId));
    }

    public ITerminalScreen Create(string platformId, string[] args)
    {
        if (Platforms.TryGetValue(platformId, out var descriptor))
            return descriptor.CreateScreen(args);
        throw new ArgumentException($"Unknown platform: {platformId}", nameof(platformId));
    }

    public bool Supports(string platformId) => Platforms.ContainsKey(platformId);

    public string? GetDefaultProfilePath(string platformId) =>
        Platforms.TryGetValue(platformId, out var descriptor) ? descriptor.DefaultProfilePath : null;

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
