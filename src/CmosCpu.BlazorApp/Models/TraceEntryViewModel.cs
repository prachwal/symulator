using CmosCpu.Core;

namespace CmosCpu.BlazorApp.Models;

public class TraceEntryViewModel
{
    public string Cycle { get; init; } = "";
    public string PC { get; init; } = "";
    public string Opcode { get; init; } = "";
    public string Mnemonic { get; init; } = "";
    public string A { get; init; } = "";
    public string X { get; init; } = "";
    public string Y { get; init; } = "";
    public string SP { get; init; } = "";
    public string Flags { get; init; } = "";
    public string Description { get; init; } = "";

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
