namespace CmosCpu.Terminal.Tui.Apple1;

public static class Apple1PromptDetector
{
    public static bool LooksLikeWozPrompt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        string trimmed = text.TrimEnd();
        if (trimmed.Length == 0)
            return false;
        return trimmed[^1] == '\\';
    }

    public static bool LooksLikeBasicPrompt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        string trimmed = text.TrimEnd();
        if (trimmed.Length == 0)
            return false;

        if (trimmed[^1] != '>')
            return false;

        int lastNewline = trimmed.LastIndexOf('\n');
        if (lastNewline < 0)
            return false;

        string afterNewline = trimmed[(lastNewline + 1)..];
        return afterNewline.Length == 1 && afterNewline[0] == '>';
    }

    public static Apple1TerminalMode DetectMode(string recentOutput, Apple1TerminalMode currentMode)
    {
        if (LooksLikeBasicPrompt(recentOutput))
            return Apple1TerminalMode.Basic;

        if (LooksLikeWozPrompt(recentOutput))
            return Apple1TerminalMode.WozMonitor;

        if (currentMode == Apple1TerminalMode.Booting || currentMode == Apple1TerminalMode.Unknown)
        {
            if (recentOutput.Contains("\\") || recentOutput.Contains(">"))
                return Apple1TerminalMode.WozMonitor;
        }

        return currentMode;
    }
}
