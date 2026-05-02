using System.ComponentModel;
using System.Runtime.CompilerServices;
using Symulator.Application.Abstractions;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1WorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IMachineSession _session;
    private string _terminalOutput = string.Empty;
    private string _inputText = string.Empty;
    private string _statusText = "Ready";
    private string _modeText = "Unknown";

    public Apple1WorkspaceViewModel(IMachineSession session)
    {
        _session = session;
    }

    public string TerminalOutput
    {
        get => _terminalOutput;
        set { _terminalOutput = value; OnPropertyChanged(); }
    }

    public string InputText
    {
        get => _inputText;
        set { _inputText = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string ModeText
    {
        get => _modeText;
        set { _modeText = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
