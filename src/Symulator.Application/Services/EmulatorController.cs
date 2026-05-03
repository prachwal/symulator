using NLog;
using Symulator.Application.Abstractions;

namespace Symulator.Application.Services;

public sealed class EmulatorController : IEmulatorController, IAsyncDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly MachineCatalog _catalog;
    private IMachineSession? _activeSession;
    private EmulatorStateSnapshot? _current;

    public EmulatorController(MachineCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public IMachineCatalog Catalog => _catalog;
    public IMachineSession? ActiveSession => _activeSession;
    public EmulatorStateSnapshot? Current => _current;

    public event EventHandler<EmulatorStateSnapshot>? StateChanged;
    public event EventHandler<string>? StatusChanged;

    public async Task<bool> SelectMachineAsync(string machineId, CancellationToken cancellationToken = default)
    {
        Logger.Debug("SelectMachineAsync requested for machineId={MachineId}", machineId);
        var module = _catalog.FindModule(machineId);
        if (module is null)
        {
            Logger.Warn("No module found for machineId={MachineId}", machineId);
            return false;
        }

        if (_activeSession is not null)
        {
            Logger.Debug("Disposing previous active session {MachineId}", _activeSession.MachineId);
            _activeSession.StateChanged -= OnSessionStateChanged;
            _activeSession.StatusChanged -= OnSessionStatusChanged;
            await _activeSession.DisposeAsync();
            _activeSession = null;
        }

        var session = await module.CreateSessionAsync(machineId, cancellationToken);
        if (session is null)
        {
            Logger.Warn("Module {ModuleId} returned null session for machineId={MachineId}", module.Id, machineId);
            return false;
        }

        _activeSession = session;
        _activeSession.StateChanged += OnSessionStateChanged;
        _activeSession.StatusChanged += OnSessionStatusChanged;

        Logger.Info("Machine selected: {MachineId} from module {ModuleId}", machineId, module.Id);
        Logger.Debug("Session created: displayName={DisplayName}", session.DisplayName);

        _current = _activeSession.Current;
        Logger.Debug("Initial snapshot after selection: hasCpu={HasCpu}, isRunning={IsRunning}, totalInstructions={TotalInstructions}", _current?.Cpu is not null, _current?.IsRunning, _current?.TotalInstructions);
        StateChanged?.Invoke(this, _current);
        StatusChanged?.Invoke(this, $"Machine selected: {session.DisplayName}");

        return true;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        Logger.Debug("ResetAsync requested; hasSession={HasSession}", _activeSession is not null);
        return _activeSession?.ResetAsync(cancellationToken) ?? Task.CompletedTask;
    }

    public Task StepInstructionAsync(CancellationToken cancellationToken = default)
    {
        Logger.Debug("StepInstructionAsync requested; hasSession={HasSession}", _activeSession is not null);
        return _activeSession?.StepInstructionAsync(cancellationToken) ?? Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_activeSession is null)
        {
            Logger.Warn("RunAsync called with no active session");
            return;
        }

        StatusChanged?.Invoke(this, "Running...");
        Logger.Debug("Delegating RunAsync to active session {MachineId}", _activeSession.MachineId);
        await _activeSession.RunAsync(cancellationToken);
    }

    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        if (_activeSession is null)
        {
            Logger.Warn("PauseAsync called with no active session");
            return;
        }

        StatusChanged?.Invoke(this, "Paused");
        Logger.Debug("Delegating PauseAsync to active session {MachineId}", _activeSession.MachineId);
        await _activeSession.PauseAsync(cancellationToken);
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        Logger.Debug("SendInputAsync requested; hasSession={HasSession}; length={Length}", _activeSession is not null, text?.Length ?? 0);
        return _activeSession?.SendInputAsync(text, cancellationToken) ?? Task.CompletedTask;
    }

    public Task<MachineCommandResult> ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default)
    {
        Logger.Debug("ExecuteMachineCommandAsync requested; hasSession={HasSession}; commandId={CommandId}; parameterType={ParameterType}", _activeSession is not null, commandId, parameter?.GetType().Name ?? "null");
        if (_activeSession is null)
            return Task.FromResult(MachineCommandResult.Failure("No active session"));

        return _activeSession.ExecuteMachineCommandAsync(commandId, parameter, cancellationToken);
    }

    private void OnSessionStateChanged(object? sender, EmulatorStateSnapshot snapshot)
    {
        _current = snapshot;
        Logger.Trace("Session state changed: machineId={MachineId}, hasCpu={HasCpu}, isRunning={IsRunning}, totalInstructions={TotalInstructions}", snapshot.MachineId, snapshot.Cpu is not null, snapshot.IsRunning, snapshot.TotalInstructions);
        StateChanged?.Invoke(this, snapshot);
    }

    private void OnSessionStatusChanged(object? sender, string status)
    {
        Logger.Trace("Session status changed: {Status}", status);
        StatusChanged?.Invoke(this, status);
    }

    public async ValueTask DisposeAsync()
    {
        if (_activeSession is not null)
        {
            _activeSession.StateChanged -= OnSessionStateChanged;
            _activeSession.StatusChanged -= OnSessionStatusChanged;
            await _activeSession.DisposeAsync();
            _activeSession = null;
        }
    }
}
