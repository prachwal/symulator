using System.Text;
using CmosCpu.Computer;

namespace CmosCpu.Computer;

public sealed class Apple1BasicBootController
{
    private readonly ComputerMachine _machine;
    private readonly Apple1PiaTerminalDevice _terminal;
    private int _outputOffset;
    private readonly StringBuilder _trace;
    private readonly int _bootTimeoutCycles;

    public string TraceLog => _trace.ToString();

    public sealed record BootResult(bool Success, string Trace, ulong CyclesUsed, bool BasicDetected);

    public Apple1BasicBootController(ComputerMachine machine, Apple1PiaTerminalDevice terminal, int bootTimeoutCycles = 1000000)
    {
        _machine = machine;
        _terminal = terminal;
        _outputOffset = terminal.OutputLength;
        _trace = new StringBuilder();
        _bootTimeoutCycles = bootTimeoutCycles;
    }

    public BootResult Boot(bool autoBasic, ushort? entryAddress = null, bool traceEnabled = false)
    {
        if (entryAddress.HasValue)
        {
            _machine.Cpu.SetProgramCounter(entryAddress.Value);
            AppendTrace($"Boot: entry PC=0x{entryAddress.Value:X4}", traceEnabled);
            return new BootResult(true, _trace.ToString(), 0, false);
        }

        _machine.Reset();
        AppendTrace($"Boot: reset PC=0x{_machine.Cpu.PC:X4}", traceEnabled);

        if (!autoBasic)
            return new BootResult(true, _trace.ToString(), 0, false);

        string initialOutput = ConsumeOutput();
        if (!string.IsNullOrEmpty(initialOutput))
            AppendTrace($"Boot: initial output: {initialOutput.Trim()}", traceEnabled);

        ulong startCycles = _machine.Cpu.CycleCount;
        bool wozOutput = false;

        while (_machine.Cpu.CycleCount - startCycles < (ulong)_bootTimeoutCycles)
        {
            if (_machine.Cpu.IsHalted) break;
            _machine.Step();

            string output = ConsumeOutput();
            if (!string.IsNullOrEmpty(output))
            {
                wozOutput = true;
                AppendTrace($"Boot: Woz output detected ({output.Trim()})", traceEnabled);
                break;
            }
        }

        if (!wozOutput)
        {
            AppendTrace("Boot: no Woz output within timeout", traceEnabled);
            return new BootResult(false, _trace.ToString(), _machine.Cpu.CycleCount - startCycles, false);
        }

        _terminal.QueueKey('E');
        _terminal.QueueKey('0');
        _terminal.QueueKey('0');
        _terminal.QueueKey('0');
        _terminal.QueueKey('R');
        _terminal.QueueKey('\r');
        AppendTrace("Boot: sending E000R", traceEnabled);

        startCycles = _machine.Cpu.CycleCount;
        bool basicOutput = false;

        while (_machine.Cpu.CycleCount - startCycles < (ulong)_bootTimeoutCycles)
        {
            if (_machine.Cpu.IsHalted) break;
            _machine.Step();

            string output = ConsumeOutput();
            if (!string.IsNullOrEmpty(output))
            {
                basicOutput = true;
                AppendTrace($"Boot: BASIC output detected ({output.Trim()})", traceEnabled);
                break;
            }
        }

        if (!basicOutput)
            AppendTrace("Boot: no BASIC output within timeout", traceEnabled);
        else
            AppendTrace("Boot: BASIC handoff completed", traceEnabled);

        return new BootResult(true, _trace.ToString(), _machine.Cpu.CycleCount, basicOutput);
    }

    private string ConsumeOutput()
    {
        string output = _terminal.ConsumeOutputSince(_outputOffset);
        _outputOffset = _terminal.OutputLength;
        return output;
    }

    private void AppendTrace(string message, bool consoleOutput)
    {
        _trace.AppendLine(message);
        if (consoleOutput)
            Console.WriteLine(message);
    }
}
