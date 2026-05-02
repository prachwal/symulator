using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1RiotStatusViewModel : INotifyPropertyChanged
{
    private string _portA = "00";
    private string _portB = "00";
    private string _ddra = "00";
    private string _ddrb = "00";

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
