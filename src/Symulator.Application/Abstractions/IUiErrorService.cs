namespace Symulator.Application.Abstractions;

public interface IUiErrorService
{
    IReadOnlyList<UiErrorEntry> Errors { get; }
    event EventHandler<UiErrorEntry>? ErrorAdded;
    void Report(Exception exception, string context);
    void Report(string message, string? details = null);
    void Clear();
}
