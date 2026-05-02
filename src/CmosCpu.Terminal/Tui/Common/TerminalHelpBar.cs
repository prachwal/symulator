using Terminal.Gui.ViewBase;

namespace CmosCpu.Terminal.Tui.Common;

public sealed class TerminalHelpBar
{
    public View View { get; }

    public TerminalHelpBar(string text)
    {
        View = new View
        {
            Text = text,
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill()
        };
    }

    public void SetText(string text) => View.Text = text;
}
