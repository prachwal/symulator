using System.Text;
using CmosCpu.Terminal.Session;
using NLog;

namespace CmosCpu.Terminal.Commands;

public static class BatchCommands
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const int ExitSuccess = 0;
    private const int ExitValidationError = 1;
    private const int ExitFileError = 2;
    private const int ExitExecutionError = 3;
    private const int ExitAsmError = 4;
    private const int ExitUnsupported = 5;

    private static bool EnsureLoaded(ISimulatorSession session)
    {
        if (session.IsLoaded)
            return true;

        const string defaultJson = """
        {
            "id": "retro70-mos6502",
            "name": "Retro70 MOS 6502 Computer",
            "cpu": "mos6502",
            "clockHz": 1000000,
            "memory": {
                "ram": [{ "start": "0x0000", "size": "0x8000" }],
                "rom": [{ "start": "0xF000", "size": "0x1000" }],
                "vectors": { "reset": "0xF000", "nmi": "0xF100", "irq": "0xF200" }
            },
            "devices": [
                { "type": "text-display", "id": "screen", "start": "0xD000", "width": 40, "height": 25 },
                { "type": "keyboard", "id": "keyboard", "dataAddress": "0xD800", "statusAddress": "0xD801" }
            ]
        }
        """;

        return session.LoadProfile(defaultJson);
    }

    public static int HandleHelp(string[] args)
    {
        if (args.Length > 0 && args[0] is "--verbose" or "-v")
        {
            Console.WriteLine("Verbose help: all commands accept --help for per-command details.");
        }
        Console.WriteLine("Symulator Terminal Client v1.0");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine("  interactive  - Start interactive TUI mode");
        Console.WriteLine("  profile      - Load machine profile");
        Console.WriteLine("  reset        - Reset CPU");
        Console.WriteLine("  step         - Execute one or more instructions");
        Console.WriteLine("  run          - Run emulation (use Ctrl+C to stop)");
        Console.WriteLine("  pause        - Pause running emulation");
        Console.WriteLine("  state        - Show CPU and session state");
        Console.WriteLine("  registers    - Show registers and flags");
        Console.WriteLine("  memory       - Show memory hex dump");
        Console.WriteLine("  stack        - Show stack");
        Console.WriteLine("  breakpoints  - Manage breakpoints");
        Console.WriteLine("  terminal     - Show terminal I/O");
        Console.WriteLine("  load         - Load binary file");
        Console.WriteLine("  load-rom     - Load ROM binary");
        Console.WriteLine("  save         - Save memory dump");
        Console.WriteLine("  asm          - Compile and load ASM file");
        Console.WriteLine("  speed        - Set batch/delay");
        Console.WriteLine("  version      - Show version");
        Console.WriteLine();
        Console.WriteLine("Use 'help --verbose' for more details.");
        return ExitSuccess;
    }

    public static int HandleVersion(string[] args)
    {
        Console.WriteLine("CmosCpu.Terminal v1.0.0");
        Console.WriteLine("Symulator - Retro Computer Emulator Terminal Client");
        Console.WriteLine(".NET 10.0 / C#");
        return ExitSuccess;
    }

    public static int HandleState(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session))
        {
            Console.Error.WriteLine("Failed to load default profile");
            return ExitValidationError;
        }

        var machine = session.Machine!;
        var cpu = machine.Cpu;

        string status = session.IsRunning ? "Running" : cpu.IsHalted ? "HALTED" : "Stopped";
        Console.WriteLine($"Status: {status}");
        Console.WriteLine($"Profile: {machine.Profile.Name} ({machine.Profile.Id})");
        Console.WriteLine($"CPU: MOS 6502");
        Console.WriteLine($"Instructions: {session.TotalInstructionsExecuted}");
        Console.WriteLine($"Cycles: {cpu.CycleCount}");
        Console.WriteLine($"PC: 0x{cpu.PC:X4}  A: 0x{cpu.A:X2}  X: 0x{cpu.X:X2}  Y: 0x{cpu.Y:X2}  SP: 0x{cpu.SP:X2}");
        Console.WriteLine($"Flags: NV-BDIZC = {(cpu.Negative ? '1' : '0')}{(cpu.Overflow ? '1' : '0')}{(cpu.Break ? '1' : '0')}{(cpu.Decimal ? '1' : '0')}{(cpu.InterruptDisable ? '1' : '0')}{(cpu.Zero ? '1' : '0')}{(cpu.Carry ? '1' : '0')}");
        Console.WriteLine($"Batch: {session.GetSpeed().batch}  Delay: {session.GetSpeed().delay}ms");

        if (!string.IsNullOrEmpty(session.LastError))
            Console.WriteLine($"Error: {session.LastError}");

        return ExitSuccess;
    }

    public static int HandleProfile(ISimulatorSession session, string[] args)
    {
        string? file = GetArgValue(args, "--file");
        string? jsonOrFile = GetArgValue(args, "--profile");

        string? source = file ?? jsonOrFile;

        if (source is null)
        {
            // Use default Retro70 profile
            const string defaultJson = """
            {
                "id": "retro70-mos6502",
                "name": "Retro70 MOS 6502 Computer",
                "cpu": "mos6502",
                "clockHz": 1000000,
                "memory": {
                    "ram": [{ "start": "0x0000", "size": "0x8000" }],
                    "rom": [{ "start": "0xF000", "size": "0x1000" }],
                    "vectors": { "reset": "0xF000", "nmi": "0xF100", "irq": "0xF200" }
                },
                "devices": [
                    { "type": "text-display", "id": "screen", "start": "0xD000", "width": 40, "height": 25 },
                    { "type": "keyboard", "id": "keyboard", "dataAddress": "0xD800", "statusAddress": "0xD801" }
                ]
            }
            """;
            if (!session.LoadProfile(defaultJson))
            {
                Console.Error.WriteLine("Failed to load default profile");
                return ExitExecutionError;
            }
            Console.WriteLine("Loaded default Retro70 profile");
            return ExitSuccess;
        }

        bool ok;
        if (source.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && File.Exists(source))
            ok = session.LoadProfileFromFile(source);
        else
            ok = session.LoadProfile(source);

        if (!ok)
        {
            Console.Error.WriteLine($"Failed to load profile: {source}");
            return ExitFileError;
        }

        Console.WriteLine($"Loaded profile: {session.Machine?.Profile.Name ?? source}");
        return ExitSuccess;
    }

    public static int HandleReset(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session))
        {
            Console.Error.WriteLine("No profile loaded");
            return ExitValidationError;
        }
        session.Reset();
        Console.WriteLine("CPU reset");
        return ExitSuccess;
    }

    public static int HandleStep(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session))
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        int count = 1;
        string? countStr = GetArgValue(args, "--count") ?? GetArgValue(args, "-n");
        if (countStr is not null && int.TryParse(countStr, out int parsed) && parsed > 0)
            count = parsed;

        if (session.IsRunning)
        {
            Console.Error.WriteLine("Cannot step while running. Pause first.");
            return ExitExecutionError;
        }

        for (int i = 0; i < count; i++)
        {
            if (session.Machine!.Cpu.IsHalted)
            {
                Console.WriteLine("CPU is HALTED");
                break;
            }
            int cycles = session.Step();
            if (i == 0 && cycles <= 0)
            {
                Console.WriteLine("No instruction executed (HALTED)");
                break;
            }
        }

        Console.WriteLine($"Executed {count} instruction(s). PC=0x{session.Machine!.Cpu.PC:X4}");
        return ExitSuccess;
    }

    public static int HandleRun(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session))
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        if (session.IsRunning)
        {
            Console.WriteLine("Already running");
            return ExitSuccess;
        }

        Console.WriteLine("Starting emulation... Press Ctrl+C to stop.");
        Logger.Info("Batch run started");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            var task = session.StartAsync();
            task.Wait();
        }
        catch (AggregateException ae) when (ae.InnerException is OperationCanceledException)
        {
            Logger.Info("Run cancelled by user");
        }
        finally
        {
            session.Stop();
            Console.WriteLine();
            Console.WriteLine($"Stopped. Instructions executed: {session.TotalInstructionsExecuted}");
        }

        return ExitSuccess;
    }

    public static int HandlePause(ISimulatorSession session, string[] args)
    {
        if (!session.IsRunning)
        {
            Console.WriteLine("Not running");
            return ExitSuccess;
        }
        session.Stop();
        Console.WriteLine("Paused");
        return ExitSuccess;
    }

    public static int HandleRegisters(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session) || session.Machine is null)
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        Views.RegistersView.Render(session.Machine.Cpu);
        return ExitSuccess;
    }

    public static int HandleMemory(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session) || session.Machine is null)
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        string? fromStr = GetArgValue(args, "--from") ?? GetArgValue(args, "-f") ?? "0x0000";
        string? lengthStr = GetArgValue(args, "--length") ?? GetArgValue(args, "-l") ?? "256";

        if (!AddressParser.TryParse(fromStr, out ushort from))
        {
            Console.Error.WriteLine($"Invalid address: {fromStr}");
            return ExitValidationError;
        }

        if (!int.TryParse(lengthStr, out int length) || length <= 0)
        {
            Console.Error.WriteLine($"Invalid length: {lengthStr}");
            return ExitValidationError;
        }

        length = Math.Min(length, 65536);
        byte[] data = session.ReadMemory(from, length);
        Views.MemoryView.Render(data, from);

        return ExitSuccess;
    }

    public static int HandleStack(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session) || session.Machine is null)
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        Views.StackView.Render(session.Machine.Memory, session.Machine.Cpu.SP);
        return ExitSuccess;
    }

    public static int HandleTerminal(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session) || session.Machine is null)
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        if (session.Machine.TextDisplay is not null)
            Views.TerminalIoView.RenderTextDisplay(session.Machine.TextDisplay);

        if (session.Machine.Apple1Terminal is not null)
            Views.TerminalIoView.RenderApple1Terminal(session.Machine.Apple1Terminal);

        if (session.Machine.TextDisplay is null && session.Machine.Apple1Terminal is null)
            Console.WriteLine("No terminal device in current profile");

        return ExitSuccess;
    }

    public static int HandleLoad(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session))
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        string? file = GetArgValue(args, "--file") ?? GetArgValue(args, "-f");
        string? addrStr = GetArgValue(args, "--addr") ?? GetArgValue(args, "-a") ?? "0x0000";

        if (file is null)
        {
            Console.Error.WriteLine("Usage: load --file <path> --addr <address>");
            return ExitValidationError;
        }

        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"File not found: {file}");
            return ExitFileError;
        }

        if (!AddressParser.TryParse(addrStr, out ushort addr))
        {
            Console.Error.WriteLine($"Invalid address: {addrStr}");
            return ExitValidationError;
        }

        try
        {
            byte[] data = File.ReadAllBytes(file);
            session.LoadBinary(data, addr);
            Console.WriteLine($"Loaded {data.Length} bytes at 0x{addr:X4} from {file}");
            return ExitSuccess;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load file: {ex.Message}");
            return ExitFileError;
        }
    }

    public static int HandleLoadRom(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session))
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        string? file = GetArgValue(args, "--file") ?? GetArgValue(args, "-f");
        string? addrStr = GetArgValue(args, "--addr") ?? GetArgValue(args, "-a") ?? "0xF000";

        if (file is null)
        {
            Console.Error.WriteLine("Usage: load-rom --file <path> --addr <address>");
            return ExitValidationError;
        }

        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"File not found: {file}");
            return ExitFileError;
        }

        if (!AddressParser.TryParse(addrStr, out ushort addr))
        {
            Console.Error.WriteLine($"Invalid address: {addrStr}");
            return ExitValidationError;
        }

        try
        {
            byte[] data = File.ReadAllBytes(file);
            session.LoadBinaryToRom(data, addr);
            Console.WriteLine($"Loaded ROM {data.Length} bytes at 0x{addr:X4} from {file}");
            return ExitSuccess;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load ROM: {ex.Message}");
            return ExitFileError;
        }
    }

    public static int HandleSave(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session))
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        string? file = GetArgValue(args, "--file") ?? GetArgValue(args, "-f");
        string? fromStr = GetArgValue(args, "--from") ?? "0x0000";
        string? lengthStr = GetArgValue(args, "--length") ?? "256";

        if (file is null)
        {
            Console.Error.WriteLine("Usage: save --file <path> --from <addr> --length <N>");
            return ExitValidationError;
        }

        if (!AddressParser.TryParse(fromStr, out ushort from))
        {
            Console.Error.WriteLine($"Invalid address: {fromStr}");
            return ExitValidationError;
        }

        if (!int.TryParse(lengthStr, out int length) || length <= 0)
        {
            Console.Error.WriteLine($"Invalid length: {lengthStr}");
            return ExitValidationError;
        }

        try
        {
            byte[] data = session.ReadMemory(from, Math.Min(length, 65536));
            File.WriteAllBytes(file, data);
            Console.WriteLine($"Saved {data.Length} bytes from 0x{from:X4} to {file}");
            return ExitSuccess;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save: {ex.Message}");
            return ExitFileError;
        }
    }

    public static int HandleAsm(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session))
        {
            Console.Error.WriteLine("Failed to load profile");
            return ExitValidationError;
        }

        string? file = GetArgValue(args, "--file") ?? GetArgValue(args, "-f");
        string? source = GetArgValue(args, "--source") ?? GetArgValue(args, "-s");

        string? code = null;

        if (file is not null)
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"File not found: {file}");
                return ExitFileError;
            }
            code = File.ReadAllText(file);
        }
        else if (source is not null)
        {
            code = source;
        }
        else
        {
            Console.Error.WriteLine("Usage: asm --file <path> or asm --source \"code\"");
            return ExitValidationError;
        }

        if (!session.CompileAndLoad(code))
        {
            Console.Error.WriteLine("Assembly failed");
            return ExitAsmError;
        }

        Console.WriteLine("Assembly successful. Program loaded at 0x8000.");
        return ExitSuccess;
    }

    public static int HandleSpeed(ISimulatorSession session, string[] args)
    {
        string? batchStr = GetArgValue(args, "--batch") ?? GetArgValue(args, "-b");
        string? delayStr = GetArgValue(args, "--delay") ?? GetArgValue(args, "-d");

        if (batchStr is not null && int.TryParse(batchStr, out int batch) && batch > 0)
        {
            int delay = session.GetSpeed().delay;
            if (delayStr is not null && int.TryParse(delayStr, out int d) && d > 0)
                delay = d;
            session.SetSpeed(batch, delay);
            Console.WriteLine($"Speed set: batch={batch}, delay={delay}ms");
        }
        else
        {
            var (b, d) = session.GetSpeed();
            Console.WriteLine($"Current speed: batch={b}, delay={d}ms");
        }

        return ExitSuccess;
    }

    public static int HandleBreakpoints(ISimulatorSession session, string[] args)
    {
        if (!EnsureLoaded(session) || session.Machine is null)
        {
            Console.Error.WriteLine("No profile loaded");
            return ExitValidationError;
        }

        string? action = args.Length > 0 ? args[0] : "list";

        switch (action.ToLowerInvariant())
        {
            case "list":
                Console.WriteLine("Breakpoints: (not implemented in CPU core)");
                break;
            default:
                Console.Error.WriteLine("Unknown breakpoint action. Use: list");
                return ExitValidationError;
        }

        return ExitSuccess;
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
