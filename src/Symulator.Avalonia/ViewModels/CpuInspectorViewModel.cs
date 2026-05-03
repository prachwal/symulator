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
    private string _machineName = "-";
    private string _machineState = "No machine selected";
    private string _bootMode = "-";
    private string _lastError = "-";
    private bool _hasLastError;

    public string Pc { get => _pc; set => SetProperty(ref _pc, value); }
    public string Sp { get => _sp; set => SetProperty(ref _sp, value); }
    public string A { get => _a; set => SetProperty(ref _a, value); }
    public string X { get => _x; set => SetProperty(ref _x, value); }
    public string Y { get => _y; set => SetProperty(ref _y, value); }
    public string Flags { get => _flags; set => SetProperty(ref _flags, value); }
    public string Cycles { get => _cycles; set => SetProperty(ref _cycles, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string MachineName { get => _machineName; set => SetProperty(ref _machineName, value); }
    public string MachineState { get => _machineState; set => SetProperty(ref _machineState, value); }
    public string BootMode { get => _bootMode; set => SetProperty(ref _bootMode, value); }
    public string LastError { get => _lastError; set => SetProperty(ref _lastError, value); }
    public bool HasLastError { get => _hasLastError; set => SetProperty(ref _hasLastError, value); }

    public void UpdateFromSnapshot(CpuStateSnapshot? cpu)
    {
        if (cpu is null)
        {
            Pc = Sp = A = X = Y = Flags = Cycles = "--";
            Status = "CPU not initialized";
            BootMode = "Not booted";
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

    public void SetMachineInfo(string name, string state, string bootMode)
    {
        MachineName = name;
        MachineState = state;
        BootMode = bootMode;
    }

    public void SetLastError(string error)
    {
        LastError = error;
        HasLastError = !string.IsNullOrEmpty(error) && error != "-";
    }

    public void SetUnavailable()
    {
        Pc = Sp = A = X = Y = Flags = Cycles = "--";
        Status = "CPU unavailable - no machine selected";
        MachineName = "-";
        MachineState = "No machine selected";
        BootMode = "-";
        LastError = "-";
        HasLastError = false;
    }
}
