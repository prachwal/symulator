namespace CmosCpu.WpfApp.Services;

public interface IDispatcherService
{
    void Invoke(Action action);
    Task InvokeAsync(Func<Task> action);
}
