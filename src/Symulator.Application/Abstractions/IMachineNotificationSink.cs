namespace Symulator.Application.Abstractions;

public interface IMachineNotificationSink
{
    void Info(string message);
    void Warning(string message, string? details = null);
    void Error(Exception exception, string context);
    void Error(string message, string? details = null);
}
