using Symulator.Application.Abstractions;

namespace Symulator.Machines.Apple1.Module;

public sealed class Apple1MachineModule : IMachineModule
{
    private readonly IMachineNotificationSink? _notificationSink;
    private readonly IUiDispatcher? _uiDispatcher;

    public Apple1MachineModule(IMachineNotificationSink? notificationSink = null, IUiDispatcher? uiDispatcher = null)
    {
        _notificationSink = notificationSink;
        _uiDispatcher = uiDispatcher;
    }

    public string Id => "apple1";
    public string DisplayName => "Apple-1";
    public string Family => "Apple";
    public string Description => "Apple-1 (1976) with Woz Monitor and BASIC";

    public IReadOnlyList<MachineDescriptor> GetMachines()
    {
        return new List<MachineDescriptor>
        {
            new("apple1", "Apple-1", "Apple", "profiles/apple1.json", "Woz Monitor and BASIC terminal system")
        };
    }

    public Task<IMachineSession> CreateSessionAsync(string machineId, CancellationToken cancellationToken = default)
    {
        if (machineId != "apple1")
            throw new ArgumentException($"Unknown machine: {machineId}", nameof(machineId));

        var session = new Apple1MachineSession(_notificationSink, _uiDispatcher);
        return Task.FromResult<IMachineSession>(session);
    }
}
