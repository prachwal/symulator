using System.Text;
using Symulator.Application.Models;

namespace Symulator.Application.Services;

public sealed class Apple1TerminalStateMachine
{
    private readonly StringBuilder _tail = new();
    private int _maxTailChars = 256;

    public int MaxTailChars
    {
        get => _maxTailChars;
        set => _maxTailChars = value > 0 ? value : 256;
    }

    public string Tail => _tail.ToString();

    public void Feed(string output)
    {
        if (string.IsNullOrEmpty(output))
            return;
        _tail.Append(output);
        if (_tail.Length > _maxTailChars)
            _tail.Remove(0, _tail.Length - _maxTailChars);
    }

    public Apple1TerminalMode DetectMode(Apple1TerminalMode currentMode)
    {
        if (_tail.Length == 0)
            return currentMode;

        string tail = _tail.ToString();

        if (LooksLikeBasicPrompt(tail))
            return Apple1TerminalMode.Basic;

        if (LooksLikeWozPrompt(tail))
            return Apple1TerminalMode.WozMonitor;

        return currentMode;
    }

    private static bool LooksLikeWozPrompt(string text)
    {
        string trimmed = text.TrimEnd();
        return trimmed.Length > 0 && trimmed[^1] == '\\';
    }

    private static bool LooksLikeBasicPrompt(string text)
    {
        string trimmed = text.TrimEnd();
        if (trimmed.Length == 0 || trimmed[^1] != '>')
            return false;

        int lastNewline = trimmed.LastIndexOf('\n');
        if (lastNewline < 0)
            return false;

        string afterNewline = trimmed[(lastNewline + 1)..];
        return afterNewline.Length == 1 && afterNewline[0] == '>';
    }

    public void Reset()
    {
        _tail.Clear();
    }
}
