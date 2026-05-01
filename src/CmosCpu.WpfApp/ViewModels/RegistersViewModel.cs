using CmosCpu.Core;

namespace CmosCpu.WpfApp.ViewModels;

public class RegistersViewModel : ViewModelBase
{
    private byte _a;
    private byte _x;
    private byte _y;
    private ushort _pc;
    private ushort _sp;
    private ulong _cycles;
    private bool _halted;
    private string _currentInstruction = "";

    public byte A { get => _a; set => SetProperty(ref _a, value); }
    public byte X { get => _x; set => SetProperty(ref _x, value); }
    public byte Y { get => _y; set => SetProperty(ref _y, value); }
    public ushort PC { get => _pc; set => SetProperty(ref _pc, value); }
    public ushort SP { get => _sp; set => SetProperty(ref _sp, value); }
    public ulong Cycles { get => _cycles; set => SetProperty(ref _cycles, value); }
    public bool Halted { get => _halted; set => SetProperty(ref _halted, value); }
    public string CurrentInstruction { get => _currentInstruction; set => SetProperty(ref _currentInstruction, value); }

    public void Update(SimulatorSnapshot snap)
    {
        A = snap.Registers.A;
        X = snap.Registers.X;
        Y = snap.Registers.Y;
        PC = snap.Registers.PC;
        SP = (ushort)(0x0100 + snap.Registers.SP);
        Cycles = snap.CycleCount;
        Halted = snap.Registers.Halted;
        CurrentInstruction = snap.CurrentInstruction ?? "";
    }
}
