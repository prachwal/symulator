namespace CmosCpu.Terminal.Tui.Common;

public sealed record TerminalPlatformDescriptor(
    string PlatformId,
    string DefaultProfilePath,
    Func<string[], ITerminalScreen> CreateScreen);
