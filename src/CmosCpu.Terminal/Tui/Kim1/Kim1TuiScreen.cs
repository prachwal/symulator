using CmosCpu.Computer;
using CmosCpu.Terminal.Session;
using CmosCpu.Terminal.Tui.Common;
using Terminal.Gui.Input;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.App;

namespace CmosCpu.Terminal.Tui.Kim1;

public sealed class Kim1TuiScreen : ITerminalScreen
{
    private ISimulatorSession _session = null!;
    private ComputerMachine _machine = null!;
    private Kim1Riot6530IoDevice _riot002 = null!;
    private Kim1Riot6530IoDevice? _riot003;
    private Kim1LedDisplayState _ledDisplay = null!;
    private Kim1KeypadState _keypad = null!;

    private readonly View[] _ledViews = new View[6];
    private readonly Dictionary<string, Button> _keypadButtons = [];
    private View _riot002View = null!;
    private View _riot003View = null!;
    private TerminalCpuPanel _cpuPanel = null!;
    private TerminalHelpBar _helpBar = null!;
    private TerminalRunLoopOptions _loopOptions = TerminalRunLoopOptions.Default;
    private object _timerToken = null!;

    public void Run(ISimulatorSession session)
    {
        _session = session;
        _machine = session.Machine!;

        if (_machine.Kim1Riot is null || _machine.Kim1LedDisplay is null || _machine.Kim1Keypad is null)
        {
            Console.Error.WriteLine("KIM-1 profile missing required devices: RIOT, LED display, keypad");
            return;
        }

        _riot002 = _machine.Kim1Riot;
        _riot003 = _machine.Kim1Riot003;
        _ledDisplay = _machine.Kim1LedDisplay;
        _keypad = _machine.Kim1Keypad;

        Application.Init();
        try
        {
            var top = new Window { Title = "KIM-1 Emulator", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
            top.KeyDown += OnTopKeyDown;
            TerminalGuiColorScheme.Apply(top);

            BuildLedDisplay(top);
            BuildKeypadAndCpu(top);
            BuildRiotDiagnostics(top);
            BuildStatusAndHelp(top);

            _timerToken = Application.TimedEvents.Add(_loopOptions.RefreshInterval, OnTimer);

            Application.Run(top);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    private void BuildLedDisplay(Window top)
    {
        var ledFrame = new FrameView { Title = "LED Display", X = 0, Y = 0, Width = Dim.Fill(), Height = 4 };
        var desc = new View { Text = "6-digit 7-segment display", X = 1, Y = 0, Width = Dim.Fill() };
        ledFrame.Add(desc);

        string digits = _ledDisplay.Digits;
        for (int i = 0; i < 6; i++)
        {
            var frame = new FrameView { X = 1 + i * 8, Y = 1, Width = 7, Height = 1 };
            _ledViews[i] = new View { Text = $"  {digits[i]}  ", X = 0, Y = 0, Width = 7 };
            frame.Add(_ledViews[i]);
            ledFrame.Add(frame);
        }

        top.Add(ledFrame);
    }

    private void BuildKeypadAndCpu(Window top)
    {
        var keypadFrame = new FrameView { Title = "Keypad", X = 0, Y = 4, Width = 30, Height = 8 };

        string[] keys = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "AD", "DA", "GO", "PC", "+"];
        int[] rows = [0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 3, 3, 3];
        int[] cols = [0, 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5, 0, 1, 3];

        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            var btn = new Button { Text = key, X = cols[i] * 5, Y = rows[i], Width = 4, Height = 1 };
            string captured = key;
            btn.Accepting += (s, e) => OnKeypadPress(captured);
            keypadFrame.Add(btn);
            _keypadButtons[key] = btn;
        }

        top.Add(keypadFrame);

        _cpuPanel = new TerminalCpuPanel("CPU / State");
        _cpuPanel.Frame.X = 30;
        _cpuPanel.Frame.Y = 4;
        _cpuPanel.Frame.Width = Dim.Fill();
        _cpuPanel.Frame.Height = 8;
        top.Add(_cpuPanel.Frame);
    }

    private void BuildRiotDiagnostics(Window top)
    {
        var riotFrame = new FrameView { Title = "RIOT / Diagnostics", X = 0, Y = 12, Width = Dim.Fill(), Height = 4 };
        _riot002View = new View { Text = FormatRiotLine("6530-002", _riot002), X = 0, Y = 0, Width = Dim.Fill() };
        riotFrame.Add(_riot002View);

        if (_riot003 is not null)
        {
            _riot003View = new View { Text = FormatRiotLine("6530-003", _riot003), X = 0, Y = 1, Width = Dim.Fill() };
            riotFrame.Add(_riot003View);
        }

        top.Add(riotFrame);
    }

    private void BuildStatusAndHelp(Window top)
    {
        _helpBar = new TerminalHelpBar("Esc/Q:Exit  Keys:0-9 A-F AD DA GO PC +");
        top.Add(_helpBar.View);
    }

    private void OnTopKeyDown(object? sender, Key keyEvent)
    {
        if (keyEvent == Key.Esc || keyEvent == Key.Q)
        {
            Application.RequestStop();
            return;
        }

        string? kimKey = Kim1KeyMapper.MapToKim1Key(keyEvent.KeyCode.ToString());
        if (kimKey is not null)
            OnKeypadPress(kimKey);
    }

    private bool OnTimer()
    {
        if (!_machine.Cpu.IsHalted)
        {
            for (int i = 0; i < _loopOptions.InstructionsPerTick; i++)
            {
                if (_machine.Cpu.IsHalted) break;
                _machine.Step();
            }
        }

        UpdateDisplay();
        return true;
    }

    private void UpdateDisplay()
    {
        string digits = _ledDisplay.Digits;
        for (int i = 0; i < 6; i++)
            _ledViews[i].Text = $"  {digits[i]}  ";

        _cpuPanel.Update(
            _machine.Cpu,
            _session.TotalInstructionsExecuted,
            isRunning: !_machine.Cpu.IsHalted);

        _riot002View.Text = FormatRiotLine("6530-002", _riot002);
        if (_riot003View is not null && _riot003 is not null)
            _riot003View.Text = FormatRiotLine("6530-003", _riot003);
    }

    private static string FormatRiotLine(string name, Kim1Riot6530IoDevice riot) =>
        $"{name}: PA=0x{riot.PortAData:X2}(DDR=0x{riot.PortADdr:X2})  PB=0x{riot.PortBData:X2}(DDR=0x{riot.PortBDdr:X2})  Timer={riot.TimerValue}(/{riot.TimerPrescalerDivider})  IRQ={(riot.IrqPending ? "Y" : "N")}  Underflow={(riot.TimerUnderflow ? "Y" : "N")}";

    private void OnKeypadPress(string key)
    {
        _keypad.PressKey(key);
        _machine.Step();
        _keypad.ReleaseKey(key);
        UpdateDisplay();
    }
}
