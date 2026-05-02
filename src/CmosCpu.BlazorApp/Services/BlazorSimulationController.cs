using CmosCpu.Assembler;
using CmosCpu.BlazorApp.Models;
using CmosCpu.Core;
using CmosCpu.Runtime;
using NLog;

namespace CmosCpu.BlazorApp.Services;

public class BlazorSimulationController : IBlazorSimulationController, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly Simulator _simulator;
    private readonly SimpleAssembler _assembler = new();
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private int _speedHz = 100;

    public bool IsRunning => _runCts is not null && !_runCts.IsCancellationRequested;
    public int SpeedHz { get => _speedHz; set => _speedHz = value; }

    public event EventHandler<SimulatorSnapshot>? SnapshotChanged;
    public event EventHandler<TraceEntry>? TraceEntryAdded;
    public event EventHandler<BusTransaction>? BusTransactionAdded;
    public event EventHandler<string>? StatusChanged;

    public BlazorSimulationController(Simulator simulator)
    {
        _simulator = simulator;
        _simulator.TraceExecuted += (s, e) => TraceEntryAdded?.Invoke(this, e.Entry);
        _simulator.SnapshotChanged += (s, e) => SnapshotChanged?.Invoke(this, e);
        _simulator.Bus.Transaction += (s, e) => BusTransactionAdded?.Invoke(this, e.Transaction);
    }

    public async Task LoadAsmAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        if (!fileName.EndsWith(".asm", StringComparison.OrdinalIgnoreCase))
        {
            StatusChanged?.Invoke(this, "Invalid file type. Only .asm files are accepted.");
            return;
        }

        using var reader = new StreamReader(stream);
        string source = await reader.ReadToEndAsync(cancellationToken);

        var (binary, errors) = _assembler.Assemble(source, 0x8000);

        if (errors.Count > 0)
        {
            string msg = $"Assembly errors: {string.Join("; ", errors)}";
            Logger.Error(msg);
            StatusChanged?.Invoke(this, msg);
            return;
        }

        _simulator.Rom.Load(binary);
        _simulator.Vectors.Write(0xFFFC, 0x00);
        _simulator.Vectors.Write(0xFFFD, 0x80);
        _simulator.Reset();
        Logger.Info("Program loaded from {File}: {Bytes} bytes", fileName, binary.Length);
        StatusChanged?.Invoke(this, $"Loaded: {fileName} ({binary.Length} bytes)");
        SnapshotChanged?.Invoke(this, _simulator.GetSnapshot());
    }

    public Task LoadBinAsync(Stream stream, string fileName, ushort loadAddress, CancellationToken cancellationToken)
    {
        if (!fileName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
        {
            StatusChanged?.Invoke(this, "Invalid file type. Only .bin files are accepted.");
            return Task.CompletedTask;
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        byte[] binary = ms.ToArray();

        if (binary.Length == 0)
        {
            StatusChanged?.Invoke(this, "Empty binary file.");
            return Task.CompletedTask;
        }

        _simulator.Rom.Load(binary);
        _simulator.Vectors.Write(0xFFFC, (byte)(loadAddress & 0xFF));
        _simulator.Vectors.Write(0xFFFD, (byte)((loadAddress >> 8) & 0xFF));
        _simulator.Reset();
        Logger.Info("Binary loaded from {File}: {Bytes} bytes at {Addr:X4}", fileName, binary.Length, loadAddress);
        StatusChanged?.Invoke(this, $"Loaded: {fileName} ({binary.Length} bytes at 0x{loadAddress:X4})");
        SnapshotChanged?.Invoke(this, _simulator.GetSnapshot());
        return Task.CompletedTask;
    }

    public void Reset()
    {
        _simulator.Reset();
        Logger.Info("CPU reset");
        StatusChanged?.Invoke(this, "Reset");
        SnapshotChanged?.Invoke(this, _simulator.GetSnapshot());
    }

    public void Step()
    {
        _simulator.Step();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_runTask is not null && !_runTask.IsCompleted)
            return;

        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _runCts.Token;
        StatusChanged?.Invoke(this, "Running");
        Logger.Info("Simulation started");
        _runTask = RunLoopAsync(token);
        await Task.CompletedTask;
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
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
            _runTask = null;
            string state = _simulator.Cpu.Halted ? "HALTED" : "Paused";
            StatusChanged?.Invoke(this, state);
            Logger.Info("Simulation {State}", state);
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
        StatusChanged?.Invoke(this, "Stopped");
        SnapshotChanged?.Invoke(this, _simulator.GetSnapshot());
    }

    public SimulatorSnapshot GetSnapshot() => _simulator.GetSnapshot();

    public CompileResultViewModel CompileAsm(string source)
    {
        var (binary, errors) = _assembler.Assemble(source, 0x8000);

        if (errors.Count > 0)
        {
            return new CompileResultViewModel
            {
                Success = false,
                Errors = errors.ToList(),
                StatusMessage = "Compile failed",
            };
        }

        var hexDump = FormatHexDump(0x8000, binary);

        return new CompileResultViewModel
        {
            Success = true,
            ByteCount = binary.Length,
            Origin = 0x8000,
            HexDump = hexDump,
            StatusMessage = $"Compile OK — {binary.Length} bytes at 0x8000",
        };
    }

    public CompileResultViewModel CompileLoadResetAsm(string source)
    {
        var result = CompileAsm(source);
        if (!result.Success)
            return result;

        _simulator.Rom.Load(result.ByteCount > 0 ? _assembler.Assemble(source, 0x8000).binary : Array.Empty<byte>());
        _simulator.Vectors.Write(0xFFFC, 0x00);
        _simulator.Vectors.Write(0xFFFD, 0x80);
        _simulator.Reset();

        result.StatusMessage = $"Loaded {result.ByteCount} bytes at 0x8000, CPU reset";
        StatusChanged?.Invoke(this, result.StatusMessage);
        SnapshotChanged?.Invoke(this, _simulator.GetSnapshot());

        return result;
    }

    private static string FormatHexDump(ushort origin, byte[] data)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < data.Length; i += 16)
        {
            sb.Append($"{(ushort)(origin + i):X4}: ");
            for (int j = 0; j < 16 && i + j < data.Length; j++)
                sb.Append($"{data[i + j]:X2} ");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public void ReportStatus(string message)
    {
        StatusChanged?.Invoke(this, message);
    }

    public void Dispose()
    {
        _runCts?.Cancel();
        _runCts?.Dispose();
    }
}
