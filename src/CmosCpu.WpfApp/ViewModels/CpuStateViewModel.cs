using CmosCpu.Core;

namespace CmosCpu.WpfApp.ViewModels;

public class CpuStateViewModel : ViewModelBase
{
    private string _state = "Reset";
    private string _lastOpcode = "";

    public string State { get => _state; set => SetProperty(ref _state, value); }
    public string LastOpcode { get => _lastOpcode; set => SetProperty(ref _lastOpcode, value); }

    public void Update(SimulatorSnapshot snap)
    {
        State = snap.Registers.Halted ? "HALTED" : "Running";
        LastOpcode = snap.CurrentInstruction ?? "";
    }

    public void SetState(string state) => State = state;
}
