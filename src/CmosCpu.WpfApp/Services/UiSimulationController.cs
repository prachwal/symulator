using CmosCpu.Assembler;
using CmosCpu.Core;
using CmosCpu.Runtime;
using NLog;
using System.IO;

namespace CmosCpu.WpfApp.Services;

public class UiSimulationController : IUiSimulationController, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly Simulator _simulator;
    private readonly IDispatcherService _dispatcher;
    private readonly SimpleAssembler _assembler = new();
    private CancellationTokenSource? _runCts;
    private int _speedHz = 100;

    public bool IsRunning => _runCts is not null && !_runCts.IsCancellationRequested;
    public int SpeedHz { get => _speedHz; set => _speedHz = value; }

    public event EventHandler<SimulatorSnapshot>? SnapshotChanged;
    public event EventHandler<TraceEntry>? TraceEntryAdded;
    public event EventHandler<BusTransaction>? BusTransactionOccurred;

    public UiSimulationController(Simulator simulator, IDispatcherService dispatcher)
    {
        _simulator = simulator;
        _dispatcher = dispatcher;

        _simulator.TraceExecuted += OnTraceExecuted;
        _simulator.SnapshotChanged += OnSnapshotChanged;
        _simulator.Bus.Transaction += OnBusTransaction;
    }

    public bool LoadAsm(string path, out IReadOnlyList<string> errors)
    {
        string source = File.ReadAllText(path);
        var (binary, asmErrors) = _assembler.Assemble(source, 0x8000);
        errors = asmErrors;

        if (asmErrors.Count > 0)
        {
            Logger.Error("Assembly failed for {Path}: {Errors}", path, string.Join("; ", asmErrors));
            return false;
        }

        _simulator.Rom.Load(binary);
        _simulator.Vectors.Write(0xFFFC, 0x00);
        _simulator.Vectors.Write(0xFFFD, 0x80);
        Logger.Info("Program loaded from {Path}: {Bytes} bytes", path, binary.Length);
        return true;
    }

    public bool LoadBin(string path, ushort loadAddress, out string? error)
    {
        try
        {
            byte[] binary = File.ReadAllBytes(path);
            _simulator.Rom.Load(binary);
            _simulator.Vectors.Write(0xFFFC, (byte)(loadAddress & 0xFF));
            _simulator.Vectors.Write(0xFFFD, (byte)((loadAddress >> 8) & 0xFF));
            error = null;
            Logger.Info("Binary loaded from {Path}: {Bytes} bytes at {Addr:X4}", path, binary.Length, loadAddress);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Logger.Error(ex, "Failed to load binary from {Path}", path);
            return false;
        }
    }

    public void Reset()
    {
        _simulator.Reset();
        EmitSnapshot();
    }

    public void Step()
    {
        _simulator.Step();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _runCts.Token;

        try
        {
            while (!token.IsCancellationRequested && !_simulator.Cpu.Halted)
            {
                _simulator.Step();

                if (_speedHz > 0 && _speedHz < 10000)
                {
                    int delayMs = 1000 / _speedHz;
                    if (delayMs > 0)
                        await Task.Delay(delayMs, token);
                }

                await Task.Yield();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _runCts?.Dispose();
            _runCts = null;
        }
    }

    public void Pause()
    {
        _simulator.Pause();
        _runCts?.Cancel();
    }

    public void Stop()
    {
        _simulator.Stop();
        _runCts?.Cancel();
        _simulator.Reset();
        EmitSnapshot();
    }

    public SimulatorSnapshot GetSnapshot() => _simulator.GetSnapshot();

    private void OnTraceExecuted(object? sender, TraceEventArgs e)
    {
        _dispatcher.Invoke(() =>
            TraceEntryAdded?.Invoke(this, e.Entry));
    }

    private void OnSnapshotChanged(object? sender, SimulatorSnapshot e)
    {
        _dispatcher.Invoke(() =>
            SnapshotChanged?.Invoke(this, e));
    }

    private void OnBusTransaction(object? sender, BusTransactionEventArgs e)
    {
        _dispatcher.Invoke(() =>
            BusTransactionOccurred?.Invoke(this, e.Transaction));
    }

    private void EmitSnapshot()
    {
        SnapshotChanged?.Invoke(this, _simulator.GetSnapshot());
    }

    public void Dispose()
    {
        _simulator.TraceExecuted -= OnTraceExecuted;
        _simulator.SnapshotChanged -= OnSnapshotChanged;
        _simulator.Bus.Transaction -= OnBusTransaction;
        _runCts?.Cancel();
        _runCts?.Dispose();
    }
}
