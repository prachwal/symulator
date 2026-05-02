namespace Symulator.Application.Abstractions;

public sealed class MachineWorkspaceDescriptor : IMachineWorkspaceDescriptor
{
    public string MachineId { get; }
    public string Title { get; }
    public object ViewModel { get; }

    public MachineWorkspaceDescriptor(string machineId, string title, object viewModel)
    {
        ArgumentNullException.ThrowIfNull(machineId);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(viewModel);

        MachineId = machineId;
        Title = title;
        ViewModel = viewModel;
    }
}
