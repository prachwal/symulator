namespace CmosCpu.Computer;

public sealed class ComputerRunController : IDisposable
{
    private CancellationTokenSource? _runCts;
    private ComputerMachine? _machine;
    private long _totalInstructions;
    private string? _lastError;

    public ComputerMachine? Machine => _machine;
    public bool IsRunning => _runCts is not null && !_runCts.IsCancellationRequested;
    public long TotalInstructionsExecuted => _totalInstructions;
    public string? LastError => _lastError;

    public int InstructionsPerBatch { get; set; } = 1000;
    public int RenderDelayMs { get; set; } = 16;

    public event Action? StateChanged;

    public void SetMachine(ComputerMachine? machine)
    {
        Stop();
        _machine = machine;
        _totalInstructions = 0;
        _lastError = null;
        NotifyStateChanged();
    }

    public async Task StartAsync(CancellationToken? externalToken = null)
    {
        if (_machine is null || IsRunning) return;

        if (externalToken.HasValue && externalToken.Value.IsCancellationRequested)
            return;

        _runCts = externalToken.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(externalToken.Value)
            : new CancellationTokenSource();

        var token = _runCts.Token;
        _lastError = null;

        try
        {
            while (!token.IsCancellationRequested)
            {
                int batchSize = InstructionsPerBatch;
                for (int i = 0; i < batchSize; i++)
                {
                    if (_machine.Cpu.IsHalted)
                    {
                        _machine.Stop();
                        NotifyStateChanged();
                        return;
                    }
                    _machine.Cpu.Step();
                    _totalInstructions++;
                }

                NotifyStateChanged();
                await Task.Delay(RenderDelayMs, token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
        }
        finally
        {
            _machine?.Stop();
            _runCts?.Dispose();
            _runCts = null;
            NotifyStateChanged();
        }
    }

    public void Stop()
    {
        _runCts?.Cancel();
        _runCts?.Dispose();
        _runCts = null;
        NotifyStateChanged();
    }

    public ulong? GetCycleCount() => _machine?.Cpu.CycleCount;

    public void Dispose()
    {
        Stop();
    }

    public void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
