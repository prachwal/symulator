using CmosCpu.Computer;
using CmosCpu.Terminal.Session;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.App;

namespace CmosCpu.Terminal.Tui;

public sealed class Apple1TuiScreen : ITerminalScreen
{
    private ISimulatorSession _session = null!;
    private ComputerMachine _machine = null!;
    private Apple1PiaTerminalDevice _terminal = null!;
    private View _statusView = null!;
    private View _registersView = null!;
    private View _flagsView = null!;
    private View _instructionsView = null!;
    private TextView _terminalOutput = null!;
    private TextField _inputField = null!;
    private int _lastOutputOffset;
    private ulong _startCycle;
    private int _maxCycles = 10000000;
    private object _timerToken = null!;

    private readonly Queue<char> _inputQueue = new();
    private bool _waitingForPrompt;
    private int _cycleBudget;
    private const int CyclesPerChar = 50;

    public void Run(ISimulatorSession session)
    {
        _session = session;
        _machine = session.Machine!;
        _terminal = _machine.Apple1Terminal!;
        _lastOutputOffset = _terminal.OutputLength;
        _startCycle = _machine.Cpu.CycleCount;

        Application.Init();

        var top = new Window { Title = "Apple-1 Terminal", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        top.KeyDown += OnTopKeyDown;
        TerminalGuiColorScheme.Apply(top);

        var terminalFrame = new FrameView { Title = "Terminal Output", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Percent(65) };
        _terminalOutput = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, WordWrap = true, Text = _terminal.Text };
        terminalFrame.Add(_terminalOutput);
        top.Add(terminalFrame);

        _inputField = new TextField { X = 0, Y = Pos.Bottom(terminalFrame), Width = Dim.Fill(), Height = 1 };
        _inputField.KeyDown += OnInputKey;
        top.Add(_inputField);

        var cpuFrame = new FrameView { Title = "CPU", X = 0, Y = Pos.Bottom(_inputField), Width = Dim.Fill(), Height = 4 };
        _registersView = new View { Text = TerminalGuiRenderer.FormatRegistersLine(_machine.Cpu), X = 0, Y = 0, Width = Dim.Fill() };
        _flagsView = new View { Text = TerminalGuiRenderer.FormatFlags(_machine.Cpu), X = 0, Y = 1, Width = Dim.Fill() };
        cpuFrame.Add(_registersView, _flagsView);
        top.Add(cpuFrame);

        _statusView = new View { Text = $"Status: {TerminalGuiRenderer.FormatStatus(false, _machine.Cpu.IsHalted)}", X = 0, Y = Pos.Bottom(cpuFrame), Width = Dim.Fill() };
        _instructionsView = new View { Text = $"Instructions: {_session.TotalInstructionsExecuted}", X = 0, Y = Pos.Bottom(_statusView), Width = Dim.Fill() };
        top.Add(_statusView, _instructionsView);

        _timerToken = Application.TimedEvents.Add(TimeSpan.FromMilliseconds(16), OnTimer);

        Application.Run(top);
        Application.Shutdown();
    }

    private void OnTopKeyDown(object? sender, Key keyEvent)
    {
        if (keyEvent == Key.Esc || keyEvent == Key.Q)
            Application.RequestStop();
    }

    private bool OnTimer()
    {
        if (_machine.Cpu.IsHalted)
        {
            UpdateDisplay();
            AppendOutput("\n*** CPU HALTED ***\n");
            return true;
        }

        for (int i = 0; i < 100; i++)
        {
            _machine.Step();

            ulong elapsed = _machine.Cpu.CycleCount - _startCycle;
            if (elapsed >= (ulong)_maxCycles)
            {
                AppendOutput($"\n*** Max cycles ({_maxCycles}) reached ***\n");
                Application.RequestStop();
                return true;
            }
        }

        StreamInput();
        UpdateDisplay();
        return true;
    }

    private void StreamInput()
    {
        if (_inputQueue.Count == 0)
            return;

        _cycleBudget += 100;
        if (_cycleBudget < CyclesPerChar)
            return;

        _cycleBudget -= CyclesPerChar;

        char c = _inputQueue.Dequeue();
        _terminal.QueueKey(c);

        if (c == '\r')
            _waitingForPrompt = true;
    }

    private void UpdateDisplay()
    {
        string output = _terminal.ConsumeOutputSince(_lastOutputOffset);
        if (!string.IsNullOrEmpty(output))
        {
            AppendOutput(output);
            if (_waitingForPrompt && IsPrompt(output))
            {
                _waitingForPrompt = false;
                _inputField.Visible = true;
                _inputField.SetFocus();
            }
        }

        _registersView.Text = TerminalGuiRenderer.FormatRegistersLine(_machine.Cpu);
        _flagsView.Text = TerminalGuiRenderer.FormatFlags(_machine.Cpu);
        _statusView.Text = $"Status: {TerminalGuiRenderer.FormatStatus(false, _machine.Cpu.IsHalted)}";
        _instructionsView.Text = $"Instructions: {_session.TotalInstructionsExecuted}";
    }

    private void AppendOutput(string text)
    {
        _terminalOutput.Text += text;
        _lastOutputOffset = _terminal.OutputLength;
    }

    private static bool IsPrompt(string output)
    {
        return output.TrimEnd().EndsWith('>');
    }

    private void OnInputKey(object? sender, Key keyEvent)
    {
        if (keyEvent == Key.Enter)
        {
            string text = _inputField.Text.ToString();
            _inputField.Text = string.Empty;

            foreach (char c in text)
                _inputQueue.Enqueue(c);
            _inputQueue.Enqueue('\r');

            _inputField.Visible = false;
            _waitingForPrompt = true;
            _cycleBudget = 0;
            keyEvent.Handled = true;
        }
    }
}
