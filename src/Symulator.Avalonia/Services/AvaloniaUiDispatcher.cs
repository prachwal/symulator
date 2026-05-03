using Avalonia.Threading;
using Symulator.Application.Abstractions;

namespace Symulator.Avalonia.Services;

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
