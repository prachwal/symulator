using Avalonia.Controls;
using Avalonia.Input;
using Symulator.Machines.MinimalBlink.Module;

namespace Symulator.Machines.MinimalBlink.Views;

public partial class MinimalBlinkWorkspaceView : UserControl
{
    public MinimalBlinkWorkspaceView()
    {
        InitializeComponent();
    }

    private async void OnTerminalKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (DataContext is MinimalBlinkWorkspaceViewModel vm)
            await vm.SendTerminalInputAsync();

        e.Handled = true;
    }
}
