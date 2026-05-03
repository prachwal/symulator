using Avalonia.Controls;
using Avalonia.Input;

namespace Symulator.Avalonia.Controls;

public partial class SerialTerminalControl : UserControl
{
    public SerialTerminalControl()
    {
        InitializeComponent();
    }

    private async void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
    }
}
