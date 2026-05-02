using CmosCpu.Terminal.Session;

namespace CmosCpu.Terminal.Tui.Common;

public static class TerminalProfileLoader
{
    public static bool LoadIfNeeded(ISimulatorSession session, string profilePath)
    {
        if (session.IsLoaded)
            return true;

        if (File.Exists(profilePath))
            return session.LoadProfileFromFile(profilePath);

        Console.Error.WriteLine($"Profile not found: {profilePath}");
        return false;
    }
}
