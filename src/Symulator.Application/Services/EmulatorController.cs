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
        var module = _catalog.FindModule(machineId);
        if (module is null)
            return false;

        if (_activeSession is not null)
        {
            _activeSession.StateChanged -= OnSessionStateChanged;
            _activeSession.StatusChanged -= OnSessionStatusChanged;
            await _activeSession.DisposeAsync();
            _activeSession = null;
        }

        var session = await module.CreateSessionAsync(machineId, cancellationToken);
        if (session is null)
            return false;

        _activeSession = session;
        _activeSession.StateChanged += OnSessionStateChanged;
        _activeSession.StatusChanged += OnSessionStatusChanged;

        Logger.Info("Machine selected: {MachineId} from module {ModuleId}", machineId, module.Id);

        await _activeSession.ResetAsync(cancellationToken);
        _current = _activeSession.Current;
        StateChanged?.Invoke(this, _current);
        StatusChanged?.Invoke(this, $"Machine selected: {session.DisplayName}");

        return true;
    }

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        return _activeSession?.ResetAsync(cancellationToken) ?? Task.CompletedTask;
    }

    public Task StepInstructionAsync(CancellationToken cancellationToken = default)
    {
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
        await _activeSession.PauseAsync(cancellationToken);
    }

    public Task SendInputAsync(string text, CancellationToken cancellationToken = default)
    {
        return _activeSession?.SendInputAsync(text, cancellationToken) ?? Task.CompletedTask;
    }

    public Task<MachineCommandResult> ExecuteMachineCommandAsync(string commandId, object? parameter = null, CancellationToken cancellationToken = default)
    {
        if (_activeSession is null)
            return Task.FromResult(MachineCommandResult.Failure("No active session"));

        return _activeSession.ExecuteMachineCommandAsync(commandId, parameter, cancellationToken);
    }

    private void OnSessionStateChanged(object? sender, EmulatorStateSnapshot snapshot)
    {
        _current = snapshot;
        StateChanged?.Invoke(this, snapshot);
    }

    private void OnSessionStatusChanged(object? sender, string status)
    {
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
