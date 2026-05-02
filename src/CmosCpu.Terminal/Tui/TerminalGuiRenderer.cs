using CmosCpu.Cpu;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace CmosCpu.Terminal.Tui;

public static class TerminalGuiRenderer
{
    public static string FormatRegistersLine(Mos6502Cpu cpu) => $"PC=0x{cpu.PC:X4}  A=0x{cpu.A:X2}  X=0x{cpu.X:X2}  Y=0x{cpu.Y:X2}  SP=0x{cpu.SP:X2}  Cycles={cpu.CycleCount}";

    public static string FormatFlags(Mos6502Cpu cpu) => $"NV-BDIZC = {(cpu.Negative ? '1' : '0')}{(cpu.Overflow ? '1' : '0')}{(cpu.Break ? '1' : '0')}{(cpu.Decimal ? '1' : '0')}{(cpu.InterruptDisable ? '1' : '0')}{(cpu.Zero ? '1' : '0')}{(cpu.Carry ? '1' : '0')}";

    public static string FormatStatus(bool isRunning, bool isHalted) => isRunning ? "RUNNING" : isHalted ? "HALTED" : "STOPPED";

    public static View CreateLabel(string text, int x, int y, int width = 0)
    {
        var label = new View { Text = text, X = x, Y = y };
        if (width > 0)
            label.Width = width;
        return label;
    }

    public static FrameView CreateFrame(string title, int x, int y, int width, int height)
    {
        return new FrameView { Title = title, X = x, Y = y, Width = width, Height = height };
    }
}
