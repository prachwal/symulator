namespace Symulator.Application.Abstractions;

public interface IMachineWorkspaceDescriptor
{
    string MachineId { get; }
    string Title { get; }
    object ViewModel { get; }
}
