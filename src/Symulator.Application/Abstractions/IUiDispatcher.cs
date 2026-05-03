namespace Symulator.Application.Abstractions;

public interface IUiDispatcher
{
    void Post(Action action);
}
