using System.IO;

namespace CmosCpu.Terminal.Tui.Apple1;

public sealed class Apple1InputCoordinator
{
    private readonly Queue<char> _userQueue = new();
    private string? _pendingUserLine;
    private bool _waitingForBasicPrompt;
    private readonly Apple1TerminalStateMachine _stateMachine = new();

    public Apple1TerminalMode Mode { get; private set; } = Apple1TerminalMode.Unknown;
    public bool TraceState { get; set; }
    public TextWriter? TraceWriter { get; set; }

    public bool HasSystemInput => false;
    public int UserQueueCount => _userQueue.Count;
    public bool HasPendingUserLine => _pendingUserLine is not null;
    public bool WaitingForPrompt => _waitingForBasicPrompt;

    public void OnOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
            return;

        _stateMachine.Feed(output);
        var detected = _stateMachine.DetectMode(Mode);

        if (detected != Mode)
        {
            string oldMode = Mode.ToString();
            Mode = detected;
            Trace($"State: {oldMode} -> {Mode}");

            if (Mode == Apple1TerminalMode.Basic)
            {
                _waitingForBasicPrompt = false;
                ReleasePendingLine();
            }
        }

        if (_waitingForBasicPrompt && Mode == Apple1TerminalMode.Basic)
        {
            _waitingForBasicPrompt = false;
            ReleasePendingLine();
        }
    }

    public void EnqueueUserLine(string line)
    {
        if (Mode == Apple1TerminalMode.Basic)
        {
            Trace($"Input: queueing line: {line}");
            foreach (char c in line)
                _userQueue.Enqueue(c);
            _userQueue.Enqueue('\r');
            _waitingForBasicPrompt = true;
        }
        else
        {
            Trace($"Input: buffering line (mode={Mode}): {line}");
            _pendingUserLine = line;
        }
    }

    public bool TryDequeueNextChar(out char c)
    {
        if (_userQueue.Count > 0)
        {
            c = _userQueue.Dequeue();
            return true;
        }

        c = default;
        return false;
    }

    public void Reset()
    {
        Mode = Apple1TerminalMode.Unknown;
        _userQueue.Clear();
        _pendingUserLine = null;
        _waitingForBasicPrompt = false;
        _stateMachine.Reset();
    }

    private void ReleasePendingLine()
    {
        if (_pendingUserLine is null)
            return;

        string line = _pendingUserLine;
        _pendingUserLine = null;
        Trace($"Input: releasing queued line: {line}");
        foreach (char c in line)
            _userQueue.Enqueue(c);
        _userQueue.Enqueue('\r');
        _waitingForBasicPrompt = true;
    }

    private void Trace(string message)
    {
        if (!TraceState)
            return;

        if (TraceWriter is not null)
            TraceWriter.WriteLine(message);
        else
            Console.Error.WriteLine(message);
    }
}
