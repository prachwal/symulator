using NLog;
using Symulator.Application.Abstractions;

namespace Symulator.Application.Services;

public sealed class UiErrorService : IUiErrorService, IMachineNotificationSink
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<UiErrorEntry> _errors = new();
    private readonly object _lock = new();

    public IReadOnlyList<UiErrorEntry> Errors
    {
        get { lock (_lock) return _errors.ToList(); }
    }

    public event EventHandler<UiErrorEntry>? ErrorAdded;

    public void Info(string message)
    {
        Logger.Debug("Machine/UI info: {Message}", message);
        Logger.Info(message);
    }

    public void Warning(string message, string? details = null)
    {
        Logger.Debug("Machine/UI warning: {Message}; details={Details}", message, details ?? "(none)");
        Report(message, details);
    }

    public void Error(Exception exception, string context)
    {
        Logger.Debug(exception, "Machine/UI error reported for context {Context}", context);
        Report(exception, context);
    }

    public void Error(string message, string? details = null)
    {
        Logger.Debug("Machine/UI error message: {Message}; details={Details}", message, details ?? "(none)");
        Report(message, details);
    }

    public void Report(Exception exception, string context)
    {
        Logger.Error(exception, "UI error in {Context}", context);

        var entry = new UiErrorEntry(
            DateTimeOffset.Now,
            "Error",
            $"[{context}] {exception.Message}",
            exception.ToString(),
            exception.GetType().Name,
            exception.StackTrace);

        lock (_lock) _errors.Add(entry);
        ErrorAdded?.Invoke(this, entry);
    }

    public void Report(string message, string? details = null)
    {
        Logger.Warn("UI warning: {Message}. Details: {Details}", message, details ?? "(none)");

        var entry = new UiErrorEntry(
            DateTimeOffset.Now,
            "Warning",
            message,
            details,
            null,
            null);

        lock (_lock) _errors.Add(entry);
        ErrorAdded?.Invoke(this, entry);
    }

    public void Clear()
    {
        lock (_lock) _errors.Clear();
    }
}
