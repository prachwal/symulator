using Symulator.Application.Abstractions;

namespace Symulator.Machines.Kim1.Module;

public sealed class Kim1MachineModule : IMachineModule
{
    private readonly IMachineNotificationSink? _notificationSink;
    private readonly IUiDispatcher? _uiDispatcher;

    public Kim1MachineModule(IMachineNotificationSink? notificationSink = null, IUiDispatcher? uiDispatcher = null)
    {
        _notificationSink = notificationSink;
        _uiDispatcher = uiDispatcher;
    }

    public string Id => "kim1";
    public string DisplayName => "KIM-1";
    public string Family => "MOS";
    public string Description => "MOS KIM-1 (1976) with keypad and LED display";

    public IReadOnlyList<MachineDescriptor> GetMachines()
    {
        return new List<MachineDescriptor>
        {
            new("kim1", "KIM-1", "MOS", "profiles/kim1.json", "Hex keypad, LED display and RIOT monitor panel")
        };
    }

    public Task<IMachineSession> CreateSessionAsync(string machineId, CancellationToken cancellationToken = default)
    {
        if (machineId != "kim1")
            throw new ArgumentException($"Unknown machine: {machineId}", nameof(machineId));

        var session = new Kim1MachineSession(_notificationSink, _uiDispatcher);
        return Task.FromResult<IMachineSession>(session);
    }
}
