using System.Text;
using CmosCpu.Computer;
using CmosCpu.Core;
using NLog;

namespace CmosCpu.Terminal.Tui;

public sealed class Apple1InteractiveTerminal
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly ComputerMachine _machine;
    private readonly string[] _args;
    private string? _scriptPath;
    private ushort _entryAddress;
    private int _maxCycles;
    private bool _echoInput;
    private bool _crlfApple1;
    private long _lastVersion;
    private ulong _startCycle;

    public Apple1InteractiveTerminal(ComputerMachine machine, string[] args)
    {
        _machine = machine;
        _args = args;

        _scriptPath = GetArgValue(args, "--script");
        var entryStr = GetArgValue(args, "--entry");
        _entryAddress = entryStr is not null && AddressParser.TryParse(entryStr, out var addr) ? addr : (ushort)0;
        var maxCyclesStr = GetArgValue(args, "--max-cycles");
        _maxCycles = maxCyclesStr is not null && int.TryParse(maxCyclesStr, out var mc) ? mc : 2000000;
        var echoStr = GetArgValue(args, "--echo-input");
        _echoInput = echoStr is not null && bool.TryParse(echoStr, out var ei) && ei;
        var crlfStr = GetArgValue(args, "--crlf");
        _crlfApple1 = crlfStr is null || crlfStr.Equals("apple1", StringComparison.OrdinalIgnoreCase);
    }

    public int Run()
    {
        var terminal = _machine.Apple1Terminal;
        if (terminal is null)
        {
            Console.Error.WriteLine("Apple-1 PIA terminal device not found in profile");
            return 1;
        }

        if (_entryAddress != 0)
            _machine.Cpu.SetProgramCounter(_entryAddress);
        else
            _machine.Reset();

        if (_scriptPath is not null)
            return RunScriptMode(terminal);

        return RunInteractiveMode(terminal);
    }

    private int RunInteractiveMode(Apple1PiaTerminalDevice terminal)
    {
        Console.WriteLine("Apple-1 Interactive Terminal");
        Console.WriteLine("Press Ctrl+C to exit.");
        Console.WriteLine();

        _startCycle = _machine.Cpu.CycleCount;
        _lastVersion = terminal.Version;

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (_machine.Cpu.IsHalted)
                {
                    Console.WriteLine("\nCPU HALTED");
                    return 0;
                }

                ulong elapsed = _machine.Cpu.CycleCount - _startCycle;
                if (elapsed >= (ulong)_maxCycles)
                {
                    Console.WriteLine($"\nMax cycles ({_maxCycles}) reached.");
                    return 0;
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                        break;

                    char c = key.KeyChar;
                    if (_crlfApple1 && c == '\n')
                        c = '\r';
                    if (_echoInput && c >= 32)
                        Console.Write(c);

                    terminal.QueueKey(c);
                }

                _machine.Step();

                string output = terminal.ConsumeOutputSince(_lastVersion);
                if (!string.IsNullOrEmpty(output))
                {
                    Console.Write(output);
                    _lastVersion = terminal.Version;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }

        ulong totalCycles = _machine.Cpu.CycleCount - _startCycle;
        Console.WriteLine($"\nStopped. Cycles: {totalCycles}");
        return 0;
    }

    private int RunScriptMode(Apple1PiaTerminalDevice terminal)
    {
        if (!File.Exists(_scriptPath!))
        {
            Console.Error.WriteLine($"Script file not found: {_scriptPath}");
            return 1;
        }

        string script = File.ReadAllText(_scriptPath!);
        if (_crlfApple1)
            script = script.Replace("\n", "\r");

        int scriptIndex = 0;
        bool outputSeen = false;

        _startCycle = _machine.Cpu.CycleCount;
        _lastVersion = terminal.Version;

        while (true)
        {
            if (_machine.Cpu.IsHalted)
                break;

            ulong elapsed = _machine.Cpu.CycleCount - _startCycle;
            if (elapsed >= (ulong)_maxCycles)
            {
                if (!outputSeen)
                {
                    Console.Error.WriteLine("Max cycles reached with no output");
                    return 2;
                }
                break;
            }

            if (scriptIndex < script.Length)
            {
                char c = script[scriptIndex++];
                terminal.QueueKey(c);
            }

            _machine.Step();

            string output = terminal.ConsumeOutputSince(_lastVersion);
            if (!string.IsNullOrEmpty(output))
            {
                outputSeen = true;
                Console.Write(output);
                _lastVersion = terminal.Version;
            }

            if (scriptIndex >= script.Length && !outputSeen)
            {
                int idleCycles = 0;
                for (int i = 0; i < 10000; i++)
                {
                    _machine.Step();
                    string extraOutput = terminal.ConsumeOutputSince(_lastVersion);
                    if (!string.IsNullOrEmpty(extraOutput))
                    {
                        Console.Write(extraOutput);
                        _lastVersion = terminal.Version;
                        outputSeen = true;
                        break;
                    }
                    idleCycles++;
                }
                if (idleCycles >= 10000)
                    break;
                break;
            }
        }

        ulong totalCycles = _machine.Cpu.CycleCount - _startCycle;
        Logger.Info("Script completed. Cycles: {Cycles}, Output seen: {Output}",
            totalCycles, outputSeen);
        return outputSeen ? 0 : 2;
    }

    private static string? GetArgValue(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}
