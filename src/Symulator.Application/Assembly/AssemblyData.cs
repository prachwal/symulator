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
