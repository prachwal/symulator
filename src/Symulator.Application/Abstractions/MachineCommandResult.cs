namespace Symulator.Application.Abstractions;

public sealed record MachineCommandResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static MachineCommandResult Success() => new(true);
    public static MachineCommandResult Failure(string error) => new(false, error);
}
