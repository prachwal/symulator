using CmosCpu.Core;

namespace CmosCpu.WpfApp.ViewModels;

public class FlagsViewModel : ViewModelBase
{
    private bool _zero;
    private bool _carry;
    private bool _negative;
    private bool _interruptDisable;
    private string _flagsText = "";

    public bool Zero { get => _zero; set => SetProperty(ref _zero, value); }
    public bool Carry { get => _carry; set => SetProperty(ref _carry, value); }
    public bool Negative { get => _negative; set => SetProperty(ref _negative, value); }
    public bool InterruptDisable { get => _interruptDisable; set => SetProperty(ref _interruptDisable, value); }
    public string FlagsText { get => _flagsText; set => SetProperty(ref _flagsText, value); }

    public void Update(SimulatorSnapshot snap)
    {
        Zero = snap.Registers.ZeroFlag;
        Carry = snap.Registers.CarryFlag;
        Negative = snap.Registers.NegativeFlag;
        InterruptDisable = snap.Registers.InterruptDisableFlag;
        FlagsText = snap.Registers.Flags.ToString();
    }
}
