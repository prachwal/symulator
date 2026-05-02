using Symulator.Application.Abstractions;

namespace Symulator.Application.Services;

public sealed class UiErrorService : IUiErrorService
{
    private readonly List<UiErrorEntry> _errors = new();
    private readonly object _lock = new();

    public IReadOnlyList<UiErrorEntry> Errors
    {
        get { lock (_lock) return _errors.ToList(); }
    }

    public event EventHandler<UiErrorEntry>? ErrorAdded;

    public void Report(Exception exception, string context)
    {
        var entry = new UiErrorEntry(
            DateTimeOffset.Now,
            "Error",
            $"[{context}] {exception.Message}",
            exception.ToString(),
            exception.GetType().Name,
            exception.StackTrace);

        lock (_lock)
        {
            _errors.Add(entry);
        }

        ErrorAdded?.Invoke(this, entry);
    }

    public void Report(string message, string? details = null)
    {
        var entry = new UiErrorEntry(
            DateTimeOffset.Now,
            "Warning",
            message,
            details,
            null,
            null);

        lock (_lock)
        {
            _errors.Add(entry);
        }

        ErrorAdded?.Invoke(this, entry);
    }

    public void Clear()
    {
        lock (_lock)
        {
            _errors.Clear();
        }
    }
}
