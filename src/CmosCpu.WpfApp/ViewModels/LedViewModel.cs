using CmosCpu.Core;

namespace CmosCpu.WpfApp.ViewModels;

public class LedViewModel : ViewModelBase
{
    private bool _isOn;

    public bool IsOn { get => _isOn; set => SetProperty(ref _isOn, value); }
    public string StateText => IsOn ? "ON" : "OFF";

    public void Update(SimulatorSnapshot snap)
    {
        if (IsOn != snap.LedOn)
        {
            IsOn = snap.LedOn;
            OnPropertyChanged(nameof(StateText));
        }
    }
}
