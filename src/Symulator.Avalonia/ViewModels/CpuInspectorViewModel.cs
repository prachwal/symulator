using Symulator.Application.Abstractions;

namespace Symulator.Avalonia.ViewModels;

public sealed class CpuInspectorViewModel : ViewModelBase
{
    private string _pc = "--";
    private string _sp = "--";
    private string _a = "--";
    private string _x = "--";
    private string _y = "--";
    private string _flags = "--";
    private string _cycles = "--";
    private string _status = "CPU unavailable - no machine selected";

    public string Pc { get => _pc; set => SetProperty(ref _pc, value); }
    public string Sp { get => _sp; set => SetProperty(ref _sp, value); }
    public string A { get => _a; set => SetProperty(ref _a, value); }
    public string X { get => _x; set => SetProperty(ref _x, value); }
    public string Y { get => _y; set => SetProperty(ref _y, value); }
    public string Flags { get => _flags; set => SetProperty(ref _flags, value); }
    public string Cycles { get => _cycles; set => SetProperty(ref _cycles, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public void UpdateFromSnapshot(CpuStateSnapshot? cpu)
    {
        if (cpu is null)
        {
            Pc = Sp = A = X = Y = Flags = Cycles = "--";
            Status = "CPU unavailable - machine not initialized";
            return;
        }

        Pc = cpu.Pc;
        Sp = cpu.Sp;
        A = cpu.A;
        X = cpu.X;
        Y = cpu.Y;
        Flags = cpu.Flags;
        Cycles = cpu.CycleCount.ToString();
        Status = cpu.IsHalted ? "HALTED" : "Running";
    }

    public void SetUnavailable()
    {
        Pc = Sp = A = X = Y = Flags = Cycles = "--";
        Status = "CPU unavailable - no machine selected";
    }
}
