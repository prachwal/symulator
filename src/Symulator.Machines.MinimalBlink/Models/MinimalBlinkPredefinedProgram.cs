namespace Symulator.Machines.MinimalBlink.Models;

public sealed record MinimalBlinkPredefinedProgram(
    string Id,
    string Name,
    string Description,
    ushort LoadAddress,
    ushort StartAddress,
    byte[] Bytes);
