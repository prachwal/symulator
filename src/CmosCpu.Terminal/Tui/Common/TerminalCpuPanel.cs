using CmosCpu.Cpu;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace CmosCpu.Terminal.Tui.Common;

public sealed class TerminalCpuPanel
{
    private readonly View _modeView;
    private readonly View _registersView;
    private readonly View _flagsView;
    private readonly View _statusView;
    private readonly View _instructionsView;

    public FrameView Frame { get; }

    public TerminalCpuPanel(string title = "CPU / State")
    {
        Frame = new FrameView
        {
            Title = title,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _modeView = new View { Text = "Mode: UNKNOWN", X = 0, Y = 0, Width = Dim.Fill() };
        _registersView = new View { Text = "", X = 0, Y = 1, Width = Dim.Fill() };
        _flagsView = new View { Text = "", X = 0, Y = 2, Width = Dim.Fill() };
        _statusView = new View { Text = "", X = 0, Y = 3, Width = Dim.Fill() };
        _instructionsView = new View { Text = "", X = 0, Y = 4, Width = Dim.Fill() };

        Frame.Add(_modeView, _registersView, _flagsView, _statusView, _instructionsView);
    }

    public void Update(Mos6502Cpu cpu, long instructions, bool isRunning, string mode = "", bool waiting = false)
    {
        _modeView.Text = string.IsNullOrEmpty(mode)
            ? $"Mode: UNKNOWN"
            : $"Mode: {mode}{(waiting ? " (waiting)" : "")}";
        _registersView.Text = TerminalGuiRenderer.FormatRegistersLine(cpu);
        _flagsView.Text = TerminalGuiRenderer.FormatFlags(cpu);
        _statusView.Text = $"Status: {TerminalGuiRenderer.FormatStatus(isRunning, cpu.IsHalted)}";
        _instructionsView.Text = $"Instructions: {instructions}";
    }
}
