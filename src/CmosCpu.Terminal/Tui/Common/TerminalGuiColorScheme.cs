namespace CmosCpu.Terminal.Tui.Common;

public static class TerminalGuiColorScheme
{
    private static readonly global::Terminal.Gui.Drawing.Attribute NormalAttr = new()
    {
        Foreground = global::Terminal.Gui.Drawing.ColorName16.BrightGreen,
        Background = global::Terminal.Gui.Drawing.ColorName16.Black
    };
    private static readonly global::Terminal.Gui.Drawing.Attribute FocusAttr = new()
    {
        Foreground = global::Terminal.Gui.Drawing.ColorName16.White,
        Background = global::Terminal.Gui.Drawing.ColorName16.DarkGray
    };
    private static readonly global::Terminal.Gui.Drawing.Attribute ActiveAttr = new()
    {
        Foreground = global::Terminal.Gui.Drawing.ColorName16.BrightCyan,
        Background = global::Terminal.Gui.Drawing.ColorName16.Black
    };
    private static readonly global::Terminal.Gui.Drawing.Attribute EditableAttr = new()
    {
        Foreground = global::Terminal.Gui.Drawing.ColorName16.White,
        Background = global::Terminal.Gui.Drawing.ColorName16.DarkGray
    };
    private static readonly global::Terminal.Gui.Drawing.Attribute ReadOnlyAttr = new()
    {
        Foreground = global::Terminal.Gui.Drawing.ColorName16.BrightGreen,
        Background = global::Terminal.Gui.Drawing.ColorName16.Black
    };
    private static readonly global::Terminal.Gui.Drawing.Attribute HotAttr = new()
    {
        Foreground = global::Terminal.Gui.Drawing.ColorName16.BrightYellow,
        Background = global::Terminal.Gui.Drawing.ColorName16.Black
    };
    private static readonly global::Terminal.Gui.Drawing.Attribute HighlightAttr = new()
    {
        Foreground = global::Terminal.Gui.Drawing.ColorName16.Black,
        Background = global::Terminal.Gui.Drawing.ColorName16.BrightGreen
    };
    private static readonly global::Terminal.Gui.Drawing.Attribute DisabledAttr = new()
    {
        Foreground = global::Terminal.Gui.Drawing.ColorName16.DarkGray,
        Background = global::Terminal.Gui.Drawing.ColorName16.Black
    };

    public static void Apply(global::Terminal.Gui.ViewBase.View view)
    {
        view.GettingAttributeForRole += OnGettingAttribute;
    }

    private static void OnGettingAttribute(object? sender, global::Terminal.Gui.Drawing.VisualRoleEventArgs e)
    {
        var attr = e.Role switch
        {
            global::Terminal.Gui.Drawing.VisualRole.Normal => NormalAttr,
            global::Terminal.Gui.Drawing.VisualRole.HotNormal => HotAttr,
            global::Terminal.Gui.Drawing.VisualRole.Focus => FocusAttr,
            global::Terminal.Gui.Drawing.VisualRole.HotFocus => HotAttr,
            global::Terminal.Gui.Drawing.VisualRole.Active => ActiveAttr,
            global::Terminal.Gui.Drawing.VisualRole.HotActive => HotAttr,
            global::Terminal.Gui.Drawing.VisualRole.Highlight => HighlightAttr,
            global::Terminal.Gui.Drawing.VisualRole.Editable => EditableAttr,
            global::Terminal.Gui.Drawing.VisualRole.ReadOnly => ReadOnlyAttr,
            global::Terminal.Gui.Drawing.VisualRole.Disabled => DisabledAttr,
            global::Terminal.Gui.Drawing.VisualRole.Code => NormalAttr,
            _ => NormalAttr
        };
        e.Result = attr;
        e.Handled = true;
    }
}
