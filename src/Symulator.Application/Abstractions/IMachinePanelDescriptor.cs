namespace Symulator.Application.Abstractions;

public interface IMachinePanelDescriptor
{
    string Id { get; }
    string Title { get; }
    string Kind { get; }
    object ViewModel { get; }
}
