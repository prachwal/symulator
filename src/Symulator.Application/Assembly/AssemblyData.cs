namespace Symulator.Application.Assembly;

public sealed class AssemblySourceProgram
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string SourceText { get; set; } = string.Empty;
}

public sealed class AssemblyListingLine
{
    public int SourceLineNumber { get; set; }
    public string SourceText { get; set; } = string.Empty;
    public ushort? Address { get; set; }
    public byte[]? Bytes { get; set; }
    public string? Label { get; set; }
    public string? Mnemonic { get; set; }
    public string? Operand { get; set; }

    public string BytesText =>
        Bytes is null || Bytes.Length == 0
            ? string.Empty
            : string.Join(" ", Bytes.Select(x => x.ToString("X2")));

    public string InstructionText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Mnemonic))
                return string.Empty;
            return string.IsNullOrWhiteSpace(Operand)
                ? Mnemonic
                : $"{Mnemonic} {Operand}";
        }
    }

    // Set externally by the ViewModel when this line matches PC
    public string CurrentLineMarker { get; set; } = string.Empty;
}

public sealed class AssemblyProgramImage
{
    public string ProgramId { get; set; } = string.Empty;
    public ushort LoadAddress { get; set; }
    public ushort StartAddress { get; set; }
    public byte[] Bytes { get; set; } = [];
    public string HexDump { get; set; } = string.Empty;
    public IReadOnlyList<AssemblyListingLine> Listing { get; set; } = Array.Empty<AssemblyListingLine>();
}

public sealed class AssemblyDiagnostic
{
    public string Severity { get; set; } = "Error";
    public int LineNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class AssemblyResult
{
    public bool Success { get; set; }
    public AssemblyProgramImage? Image { get; set; }
    public IReadOnlyList<AssemblyDiagnostic> Diagnostics { get; set; } = Array.Empty<AssemblyDiagnostic>();
}
