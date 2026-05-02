namespace CmosCpu.Core;

public class AssemblerException : Exception
{
    public int LineNumber { get; }

    public AssemblerException(int lineNumber, string message)
        : base($"Line {lineNumber}: {message}")
    {
        LineNumber = lineNumber;
    }
}