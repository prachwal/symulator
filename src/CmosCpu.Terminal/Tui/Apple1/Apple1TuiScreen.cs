using CmosCpu.Computer;
using CmosCpu.Terminal.Session;
using CmosCpu.Terminal.Tui.Common;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.App;

namespace CmosCpu.Terminal.Tui.Apple1;

public sealed class Apple1TuiScreen : ITerminalScreen
{
    private ISimulatorSession _session = null!;
    private ComputerMachine _machine = null!;
    private Apple1PiaTerminalDevice _terminal = null!;
    private View _statusView = null!;
    private View _registersView = null!;
    private View _flagsView = null!;
    private View _instructionsView = null!;
    private View _modeView = null!;
    private View _helpView = null!;
    private TextView _terminalOutput = null!;
    private TextField _inputField = null!;
    private int _lastOutputOffset;
    private ulong _startCycle;
    private int _maxCycles = 10000000;
    private object _timerToken = null!;

    private readonly Queue<char> _inputQueue = new();
    private int _cycleBudget;
    private const int CyclesPerChar = 50;
    private readonly Apple1InputCoordinator _coordinator = new();
    private readonly TerminalTextRingBuffer _outputBuffer = new(20000);

    public bool TraceState
    {
        get => _coordinator.TraceState;
        set => _coordinator.TraceState = value;
    }

    public void Run(ISimulatorSession session)
    {
        _session = session;
        _machine = session.Machine!;
        _terminal = _machine.Apple1Terminal!;
        _lastOutputOffset = _terminal.OutputLength;
        _startCycle = _machine.Cpu.CycleCount;
        _coordinator.Reset();

        Application.Init();

        var top = new Window { Title = "Apple-1 Terminal", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        top.KeyDown += OnTopKeyDown;
        TerminalGuiColorScheme.Apply(top);

        var terminalFrame = new FrameView { Title = "Terminal Output", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Percent(55) };
        _terminalOutput = new TextView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, WordWrap = true, Text = _terminal.Text };
        terminalFrame.Add(_terminalOutput);
        top.Add(terminalFrame);

        _inputField = new TextField { X = 0, Y = Pos.Bottom(terminalFrame), Width = Dim.Fill(), Height = 1 };
        _inputField.KeyDown += OnInputKey;
        top.Add(_inputField);

        var infoFrame = new FrameView { Title = "CPU", X = 0, Y = Pos.Bottom(_inputField), Width = Dim.Fill(), Height = 3 };
        _modeView = new View { Text = $"Mode: {FormatMode(_coordinator.Mode)}", X = 0, Y = 0, Width = Dim.Fill() };
        _registersView = new View { Text = TerminalGuiRenderer.FormatRegistersLine(_machine.Cpu), X = 0, Y = 1, Width = Dim.Fill() };
        infoFrame.Add(_modeView, _registersView);
        top.Add(infoFrame);

        _flagsView = new View { Text = TerminalGuiRenderer.FormatFlags(_machine.Cpu), X = 0, Y = Pos.Bottom(infoFrame), Width = Dim.Fill() };
        top.Add(_flagsView);

        _statusView = new View { Text = $"Status: {TerminalGuiRenderer.FormatStatus(false, _machine.Cpu.IsHalted)}", X = 0, Y = Pos.Bottom(_flagsView), Width = Dim.Fill() };
        _instructionsView = new View { Text = $"Instructions: {_session.TotalInstructionsExecuted}", X = 0, Y = Pos.Bottom(_statusView), Width = Dim.Fill() };
        _helpView = new View { Text = "Esc/Q:Exit  Ctrl+L:Clear  Ctrl+R:Reset", X = 0, Y = Pos.Bottom(_instructionsView), Width = Dim.Fill() };
        top.Add(_statusView, _instructionsView, _helpView);

        _timerToken = Application.TimedEvents.Add(TimeSpan.FromMilliseconds(16), OnTimer);

        _inputField.SetFocus();

        Application.Run(top);
        Application.Shutdown();
    }

    private void OnTopKeyDown(object? sender, Key keyEvent)
    {
        if (keyEvent == Key.Esc || keyEvent == Key.Q)
            Application.RequestStop();
        else if (keyEvent == Key.L.WithCtrl)
        {
            _outputBuffer.Clear();
            _terminalOutput.Text = string.Empty;
        }
        else if (keyEvent == Key.R.WithCtrl)
            ResetMachine();
    }

    private void ResetMachine()
    {
        _machine.Reset();
        _coordinator.Reset();
        _outputBuffer.Clear();
        _terminalOutput.Text = string.Empty;
        if (_machine.Apple1Terminal is not null)
            _lastOutputOffset = _machine.Apple1Terminal.OutputLength;
        _startCycle = _machine.Cpu.CycleCount;
        AppendOutput("*** RESET ***\n");
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
        _cycleBudget += 100;

        if (_coordinator.UserQueueCount > 0)
        {
            while (_cycleBudget >= CyclesPerChar && _coordinator.TryDequeueNextChar(out char c))
            {
                _cycleBudget -= CyclesPerChar;
                _inputQueue.Enqueue(c);
            }
        }

        if (_inputQueue.Count > 0)
        {
            if (_cycleBudget < CyclesPerChar)
                return;
            _cycleBudget -= CyclesPerChar;
            char ch = _inputQueue.Dequeue();
            _terminal.QueueKey(ch);
        }
    }

    private void UpdateDisplay()
    {
        string output = _terminal.ConsumeOutputSince(_lastOutputOffset);
        if (!string.IsNullOrEmpty(output))
        {
            AppendOutput(output);
            _coordinator.OnOutput(output);
        }

        _modeView.Text = $"Mode: {FormatMode(_coordinator.Mode)}  |  Waiting: {(_coordinator.WaitingForPrompt ? "YES" : "NO")}";
        _registersView.Text = TerminalGuiRenderer.FormatRegistersLine(_machine.Cpu);
        _flagsView.Text = TerminalGuiRenderer.FormatFlags(_machine.Cpu);
        _statusView.Text = $"Status: {TerminalGuiRenderer.FormatStatus(false, _machine.Cpu.IsHalted)}";
        _instructionsView.Text = $"Instructions: {_session.TotalInstructionsExecuted}";
    }

    private static string FormatMode(Apple1TerminalMode mode) => mode switch
    {
        Apple1TerminalMode.Unknown => "UNKNOWN",
        Apple1TerminalMode.Booting => "BOOTING",
        Apple1TerminalMode.WozMonitor => "WOZ",
        Apple1TerminalMode.Basic => "BASIC",
        _ => mode.ToString()
    };

    private string GetPromptPrefix()
    {
        string mode = FormatMode(_coordinator.Mode);
        return $"{mode}> ";
    }

    private void AppendOutput(string text)
    {
        _outputBuffer.Append(text);
        _terminalOutput.Text = _outputBuffer.GetText();
        _lastOutputOffset = _terminal.OutputLength;
    }

    private void OnInputKey(object? sender, Key keyEvent)
    {
        if (keyEvent == Key.Enter)
        {
            string text = _inputField.Text.ToString();
            _inputField.Text = string.Empty;

            _coordinator.EnqueueUserLine(text);
            _cycleBudget = 0;
            keyEvent.Handled = true;
        }
    }
}
