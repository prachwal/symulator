using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1LedDisplayViewModel : INotifyPropertyChanged
{
    private string _displayText = "--------";

    public string DisplayText
    {
        get => _displayText;
        set { _displayText = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
