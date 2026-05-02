using System.ComponentModel;
using System.Runtime.CompilerServices;
using Symulator.Application.Abstractions;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1WorkspaceViewModel : INotifyPropertyChanged
{
    private readonly IMachineSession _session;
    private string _displayText = "--------";
    private string _lastKey = string.Empty;
    private string _statusText = "Ready";
    private string _portA = "00";
    private string _portB = "00";
    private string _ddra = "00";
    private string _ddrb = "00";

    public Kim1WorkspaceViewModel(IMachineSession session)
    {
        _session = session;
    }

    public string DisplayText
    {
        get => _displayText;
        set { _displayText = value; OnPropertyChanged(); }
    }

    public string LastKey
    {
        get => _lastKey;
        set { _lastKey = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string PortA { get => _portA; set { _portA = value; OnPropertyChanged(); } }
    public string PortB { get => _portB; set { _portB = value; OnPropertyChanged(); } }
    public string DDRA { get => _ddra; set { _ddra = value; OnPropertyChanged(); } }
    public string DDRB { get => _ddrb; set { _ddrb = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
