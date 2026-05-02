using System.ComponentModel;
using System.Runtime.CompilerServices;
using Symulator.Machines.Apple1.Services;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1TerminalPanelViewModel : INotifyPropertyChanged
{
    private readonly Apple1InputCoordinator _inputCoordinator;
    private string _output = string.Empty;
    private string _input = string.Empty;
    private string _mode = "Unknown";

    public Apple1TerminalPanelViewModel(Apple1InputCoordinator inputCoordinator)
    {
        _inputCoordinator = inputCoordinator;
    }

    public string Output
    {
        get => _output;
        set { _output = value; OnPropertyChanged(); }
    }

    public string Input
    {
        get => _input;
        set { _input = value; OnPropertyChanged(); }
    }

    public string Mode
    {
        get => _mode;
        set { _mode = value; OnPropertyChanged(); }
    }

    public void AppendOutput(string text)
    {
        Output += text;
        _inputCoordinator.OnOutput(text);
        Mode = _inputCoordinator.Mode.ToString();
    }

    public void ClearOutput()
    {
        Output = string.Empty;
    }

    public void SendLine(string line)
    {
        _inputCoordinator.EnqueueUserLine(line);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
