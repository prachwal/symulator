using CmosCpu.Core;

namespace CmosCpu.WpfApp.Models;

public class TraceEntryViewModel : ViewModels.ViewModelBase
{
    private string _cycle = "";
    private string _pc = "";
    private string _opcode = "";
    private string _mnemonic = "";
    private string _operands = "";
    private string _a = "";
    private string _x = "";
    private string _y = "";
    private string _sp = "";
    private string _flags = "";
    private string _description = "";

    public string Cycle { get => _cycle; set => SetProperty(ref _cycle, value); }
    public string PC { get => _pc; set => SetProperty(ref _pc, value); }
    public string Opcode { get => _opcode; set => SetProperty(ref _opcode, value); }
    public string Mnemonic { get => _mnemonic; set => SetProperty(ref _mnemonic, value); }
    public string Operands { get => _operands; set => SetProperty(ref _operands, value); }
    public string A { get => _a; set => SetProperty(ref _a, value); }
    public string X { get => _x; set => SetProperty(ref _x, value); }
    public string Y { get => _y; set => SetProperty(ref _y, value); }
    public string SP { get => _sp; set => SetProperty(ref _sp, value); }
    public string Flags { get => _flags; set => SetProperty(ref _flags, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }

    public static TraceEntryViewModel FromEntry(TraceEntry entry)
    {
        return new TraceEntryViewModel
        {
            Cycle = entry.Cycle.ToString(),
            PC = $"0x{entry.PC:X4}",
            Opcode = $"0x{entry.Opcode:X2}",
            Mnemonic = entry.Mnemonic,
            A = $"0x{entry.A:X2}",
            X = $"0x{entry.X:X2}",
            Y = $"0x{entry.Y:X2}",
            SP = $"0x{entry.SP:X4}",
            Flags = entry.Flags,
            Description = entry.Description,
        };
    }
}
