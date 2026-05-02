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
    private TextView _terminalOutput = null!;
    private View _promptView = null!;
    private TextField _inputField = null!;
    private TerminalCpuPanel _cpuPanel = null!;
    private TerminalHelpBar _helpBar = null!;
    private int _lastOutputOffset;
    private ulong _startCycle;
    private int _maxCycles = 10000000;
    private object _timerToken = null!;
    private TerminalRunLoopOptions _loopOptions = TerminalRunLoopOptions.Default;

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

        if (_machine.Apple1Terminal is null)
        {
            Console.Error.WriteLine("Apple-1 profile missing required device: apple1-pia-terminal");
            return;
        }

        _terminal = _machine.Apple1Terminal;
        _lastOutputOffset = _terminal.OutputLength;
        _startCycle = _machine.Cpu.CycleCount;
        _coordinator.Reset();
        _maxCycles = _loopOptions.MaxCycles;

        Application.Init();
        try
        {
            var top = new Window { Title = "Apple-1 Terminal", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
            top.KeyDown += OnTopKeyDown;
            TerminalGuiColorScheme.Apply(top);

            BuildLayout(top);

            _timerToken = Application.TimedEvents.Add(_loopOptions.RefreshInterval, OnTimer);
            _inputField.SetFocus();

            Application.Run(top);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    private void BuildLayout(Window top)
    {
        var terminalFrame = new FrameView
        {
            Title = "Apple-1 Display",
            X = 0,
            Y = 0,
            Width = Dim.Percent(70),
            Height = Dim.Fill(4)
        };
        _terminalOutput = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true
        };
        terminalFrame.Add(_terminalOutput);

        _cpuPanel = new TerminalCpuPanel("CPU / State");
        _cpuPanel.Frame.X = Pos.Right(terminalFrame);
        _cpuPanel.Frame.Y = 0;
        _cpuPanel.Frame.Width = Dim.Fill();
        _cpuPanel.Frame.Height = Dim.Fill(4);

        var inputFrame = new FrameView
        {
            Title = "Input",
            X = 0,
            Y = Pos.Bottom(terminalFrame),
            Width = Dim.Fill(),
            Height = 3
        };

        _promptView = new View
        {
            Text = GetPromptPrefix(),
            X = 0,
            Y = 0,
            Width = 10
        };
        _inputField = new TextField
        {
            X = Pos.Right(_promptView),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };
        _inputField.KeyDown += OnInputKey;
        inputFrame.Add(_promptView, _inputField);

        _helpBar = new TerminalHelpBar("Esc/Q:Exit  F10:Step  Ctrl+L:Clear  Ctrl+R:Reset");

        top.Add(terminalFrame, _cpuPanel.Frame, inputFrame, _helpBar.View);
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
        else if (keyEvent == Key.F10)
        {
            _machine.Step();
            UpdateDisplay();
        }
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

        for (int i = 0; i < _loopOptions.InstructionsPerTick; i++)
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

        _promptView.Text = GetPromptPrefix();

        _cpuPanel.Update(
            _machine.Cpu,
            _session.TotalInstructionsExecuted,
            isRunning: !_machine.Cpu.IsHalted,
            mode: FormatMode(_coordinator.Mode),
            waiting: _coordinator.WaitingForPrompt);
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
