namespace Symulator.Machines.Kim1.Models;

public sealed record Kim1PredefinedProgram(
    string Id,
    string Name,
    string Description,
    ushort LoadAddress,
    ushort StartAddress,
    byte[] Bytes,
    string ExpectedResultDescription);
