namespace Symulator.Application.Abstractions;

public sealed record UiErrorEntry(
    DateTimeOffset Timestamp,
    string Severity,
    string Message,
    string? Details,
    string? ExceptionType,
    string? StackTrace);
