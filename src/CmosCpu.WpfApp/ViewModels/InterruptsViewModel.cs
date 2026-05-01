using CmosCpu.Core;

namespace CmosCpu.WpfApp.ViewModels;

public class InterruptsViewModel : ViewModelBase
{
    private bool _irqPending;
    private bool _nmiPending;
    private bool _interruptDisable;
    private string _resetVector = "";
    private string _irqVector = "";
    private string _nmiVector = "";
    private string _lastInterrupt = "";

    public bool IrqPending { get => _irqPending; set => SetProperty(ref _irqPending, value); }
    public bool NmiPending { get => _nmiPending; set => SetProperty(ref _nmiPending, value); }
    public bool InterruptDisable { get => _interruptDisable; set => SetProperty(ref _interruptDisable, value); }
    public string ResetVector { get => _resetVector; set => SetProperty(ref _resetVector, value); }
    public string IrqVector { get => _irqVector; set => SetProperty(ref _irqVector, value); }
    public string NmiVector { get => _nmiVector; set => SetProperty(ref _nmiVector, value); }
    public string LastInterrupt { get => _lastInterrupt; set => SetProperty(ref _lastInterrupt, value); }

    public void Update(SimulatorSnapshot snap)
    {
        InterruptDisable = snap.Registers.InterruptDisableFlag;

        if (snap.Interrupts != null)
        {
            IrqPending = snap.Interrupts.IrqPending;
            NmiPending = snap.Interrupts.NmiPending;
            ResetVector = $"0x{snap.Interrupts.ResetVector:X4}";
            IrqVector = $"0x{snap.Interrupts.IrqVector:X4}";
            NmiVector = $"0x{snap.Interrupts.NmiVector:X4}";
        }

        if (snap.Registers.Halted)
            LastInterrupt = "HALT";
    }
}
