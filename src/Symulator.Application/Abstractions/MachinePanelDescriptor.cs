namespace Symulator.Application.Abstractions;

public sealed class MachinePanelDescriptor : IMachinePanelDescriptor
{
    public string Id { get; }
    public string Title { get; }
    public string Kind { get; }
    public object ViewModel { get; }

    public MachinePanelDescriptor(string id, string title, string kind, object viewModel)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(viewModel);

        Id = id;
        Title = title;
        Kind = kind;
        ViewModel = viewModel;
    }
}
