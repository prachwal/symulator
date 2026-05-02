using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1BootPanelViewModel : INotifyPropertyChanged
{
    private bool _isBooting;
    private string _status = "Ready";

    public bool IsBooting
    {
        get => _isBooting;
        set { _isBooting = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
