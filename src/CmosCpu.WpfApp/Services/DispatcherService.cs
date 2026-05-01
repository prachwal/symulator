using System.Windows.Threading;

namespace CmosCpu.WpfApp.Services;

public class DispatcherService : IDispatcherService
{
    private readonly Dispatcher _dispatcher;

    public DispatcherService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Invoke(Action action)
    {
        if (_dispatcher.CheckAccess())
            action();
        else
            _dispatcher.Invoke(action);
    }

    public Task InvokeAsync(Func<Task> action)
    {
        if (_dispatcher.CheckAccess())
            return action();
        return _dispatcher.InvokeAsync(action).Task;
    }
}
