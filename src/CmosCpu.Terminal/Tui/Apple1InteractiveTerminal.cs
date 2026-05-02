using CmosCpu.Computer;
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
    private int _bootTimeoutCycles;
    private bool _echoInput;
    private bool _crlfApple1;
    private bool _autoBasic;
    private bool _traceBoot;
    private bool _traceState;
    private bool _exitOnMaxCycles;
    private int _lastOutputOffset;
    private ulong _startCycle;
    private readonly Apple1InputCoordinator _coordinator = new();

    public Apple1InteractiveTerminal(ComputerMachine machine, string[] args)
    {
        _machine = machine;
        _args = args;

        _scriptPath = GetArgValue(args, "--script");
        var entryStr = GetArgValue(args, "--entry");
        _entryAddress = entryStr is not null && AddressParser.TryParse(entryStr, out var addr) ? addr : (ushort)0;
        var maxCyclesStr = GetArgValue(args, "--max-cycles");
        _maxCycles = maxCyclesStr is not null && int.TryParse(maxCyclesStr, out var mc) ? mc : 10000000;
        var bootTimeoutStr = GetArgValue(args, "--boot-timeout-cycles");
        _bootTimeoutCycles = bootTimeoutStr is not null && int.TryParse(bootTimeoutStr, out var bt) ? bt : 1000000;
        var echoStr = GetArgValue(args, "--echo-input");
        _echoInput = echoStr is not null && bool.TryParse(echoStr, out var ei) && ei;
        var crlfStr = GetArgValue(args, "--crlf");
        _crlfApple1 = crlfStr is null || crlfStr.Equals("apple1", StringComparison.OrdinalIgnoreCase);
        var autoBasicStr = GetArgValue(args, "--auto-basic");
        _autoBasic = autoBasicStr is null || !bool.TryParse(autoBasicStr, out var ab) || ab;
        var traceBootStr = GetArgValue(args, "--trace-boot");
        _traceBoot = traceBootStr is not null && bool.TryParse(traceBootStr, out var tb) && tb;
        var traceStateStr = GetArgValue(args, "--trace-state");
        _traceState = traceStateStr is not null && bool.TryParse(traceStateStr, out var ts) && ts;
        var exitOnMaxCyclesStr = GetArgValue(args, "--exit-on-max-cycles");
        bool isScriptMode = _scriptPath is not null;
        _exitOnMaxCycles = exitOnMaxCyclesStr is not null
            ? bool.TryParse(exitOnMaxCyclesStr, out var eomc) && eomc
            : isScriptMode;
    }

    public int Run()
    {
        var terminal = _machine.Apple1Terminal;
        if (terminal is null)
        {
            Console.Error.WriteLine("Apple-1 PIA terminal device not found in profile");
            return 1;
        }

        _lastOutputOffset = terminal.OutputLength;

        var bootController = new Apple1BasicBootController(_machine, terminal, _bootTimeoutCycles);

        var bootResult = bootController.Boot(_autoBasic, _entryAddress != 0 ? _entryAddress : null, _traceBoot);

        if (!bootResult.Success)
        {
            Console.Error.WriteLine("Boot failed: no Woz Monitor output detected");
            PrintBootDiagnostics(bootResult, terminal);
            return 2;
        }

        if (_entryAddress == 0 && _autoBasic && _traceBoot)
            Console.WriteLine($"Boot: BASIC handoff completed (PC=0x{_machine.Cpu.PC:X4})");

        string bootOutput = ConsumeTerminalOutput(terminal);
        if (!string.IsNullOrEmpty(bootOutput))
            Console.Write(bootOutput);

        if (_scriptPath is not null)
            return RunScriptMode(terminal);

        return RunInteractiveMode(terminal);
    }

    private string ConsumeTerminalOutput(Apple1PiaTerminalDevice terminal)
    {
        string output = terminal.ConsumeOutputSince(_lastOutputOffset);
        _lastOutputOffset = terminal.OutputLength;
        return output;
    }

    private int RunInteractiveMode(Apple1PiaTerminalDevice terminal)
    {
        Console.WriteLine("Apple-1 Interactive Terminal");
        Console.WriteLine("Press Ctrl+C to exit.");
        Console.WriteLine();

        _startCycle = _machine.Cpu.CycleCount;
        _coordinator.Reset();
        _coordinator.TraceState = _traceState;

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
                    if (_exitOnMaxCycles)
                    {
                        Console.WriteLine($"\nMax cycles ({_maxCycles}) reached.");
                        return 0;
                    }
                    _startCycle = _machine.Cpu.CycleCount;
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

                string output = ConsumeTerminalOutput(terminal);
                if (!string.IsNullOrEmpty(output))
                {
                    Console.Write(output);
                    _coordinator.OnOutput(output);
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
                    PrintBootDiagnostics(new Apple1BasicBootController.BootResult(false, "", elapsed, false), terminal);
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

            string output = ConsumeTerminalOutput(terminal);
            if (!string.IsNullOrEmpty(output))
            {
                outputSeen = true;
                Console.Write(output);
            }

            if (scriptIndex >= script.Length && !outputSeen)
            {
                bool idleDone = false;
                for (int i = 0; i < 10000; i++)
                {
                    _machine.Step();
                    string extraOutput = ConsumeTerminalOutput(terminal);
                    if (!string.IsNullOrEmpty(extraOutput))
                    {
                        Console.Write(extraOutput);
                        outputSeen = true;
                        idleDone = true;
                        break;
                    }
                }
                if (!idleDone)
                    break;
                break;
            }
        }

        ulong totalCycles = _machine.Cpu.CycleCount - _startCycle;
        Logger.Info("Script completed. Cycles: {Cycles}, Output seen: {Output}",
            totalCycles, outputSeen);
        return outputSeen ? 0 : 2;
    }

    private void PrintBootDiagnostics(Apple1BasicBootController.BootResult result, Apple1PiaTerminalDevice terminal)
    {
        Console.Error.WriteLine("=== Boot diagnostics ===");
        Console.Error.WriteLine($"PC: 0x{_machine.Cpu.PC:X4}");
        Console.Error.WriteLine($"Cycles: {result.CyclesUsed}");
        Console.Error.WriteLine($"Trace:");
        Console.Error.WriteLine(result.Trace);
        Console.Error.WriteLine($"Last terminal output: {terminal.Text}");

        if (_entryAddress != 0)
            Console.Error.WriteLine($"Entry address: 0x{_entryAddress:X4}");

        string basicRomPath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            "roms/apple-1/Apple-1 BASIC ROM.bin");
        Console.Error.WriteLine($"BASIC ROM file exists: {File.Exists("roms/apple-1/Apple-1 BASIC ROM.bin")}");

        byte[] basicCheck = _machine.Memory.GetMemoryPage(0xE000, 16);
        bool basicRomPresent = basicCheck.Any(b => b != 0 && b != 0xFF);
        Console.Error.WriteLine($"BASIC ROM data at 0xE000: {(basicRomPresent ? "present" : "zeros/FF")}");
        Console.Error.WriteLine($"BASIC ROM hex: {string.Join(" ", basicCheck.Select(b => $"{b:X2}"))}");
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
