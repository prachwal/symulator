using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1KeypadViewModel : INotifyPropertyChanged
{
    private string _lastKey = string.Empty;

    public string LastKey
    {
        get => _lastKey;
        set { _lastKey = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
