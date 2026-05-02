using System.Reflection;

namespace CmosCpu.Terminal.Commands;

public sealed class CommandRouter
{
    private readonly Dictionary<string, CommandHandler> _commands = new(StringComparer.OrdinalIgnoreCase);

    public delegate int CommandHandler(string[] args);

    public void Register(string name, CommandHandler handler)
    {
        _commands[name] = handler;
    }

    public void RegisterAlias(string alias, string targetCommand)
    {
        if (_commands.TryGetValue(targetCommand, out var handler))
            _commands[alias] = handler;
    }

    public CommandHandler? Resolve(string name)
    {
        _commands.TryGetValue(name, out var handler);
        return handler;
    }

    public IReadOnlyDictionary<string, CommandHandler> Commands => _commands;

    public int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 0;
        }

        string commandName = args[0];
        var handler = Resolve(commandName);
        if (handler is null)
        {
            Console.Error.WriteLine($"Unknown command: {commandName}. Use 'help' for available commands.");
            return 1;
        }

        try
        {
            return handler(args[1..]);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error executing '{commandName}': {ex.Message}");
            return 1;
        }
    }

    public void PrintHelp()
    {
        Console.WriteLine("Symulator Terminal Client");
        Console.WriteLine("Usage: dotnet run -- [command] [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  interactive          Start interactive TUI mode");
        Console.WriteLine("  help                 Show this help");
        Console.WriteLine("  version              Show version");
        Console.WriteLine("  state                Show emulator state");
        Console.WriteLine("  profile              Load a profile (--file path.json)");
        Console.WriteLine("  reset                Reset the CPU");
        Console.WriteLine("  step                 Execute one instruction [--count N]");
        Console.WriteLine("  run                  Start continuous execution [--max-cycles N]");
        Console.WriteLine("  pause                Pause execution");
        Console.WriteLine("  registers            Show CPU registers and flags");
        Console.WriteLine("  memory               Show memory hex dump [--from addr] [--length N]");
        Console.WriteLine("  stack                Show stack contents");
        Console.WriteLine("  breakpoints          Manage breakpoints (add/remove/list)");
        Console.WriteLine("  terminal             Show terminal I/O screen");
        Console.WriteLine("  load                 Load binary (--file path --addr 0x0000)");
        Console.WriteLine("  load-rom             Load ROM binary (--file path --addr 0x0000)");
        Console.WriteLine("  save                 Save memory dump to file (--file path --from addr --length N)");
        Console.WriteLine("  asm                  Compile and load ASM file (--file path)");
        Console.WriteLine("  speed                Set emulation speed (--batch N --delay Nms)");
        Console.WriteLine("  kim1-io              Show KIM-1 RIOT 6530 I/O state");
        Console.WriteLine("  apple1-basic         Apple-1 BASIC interactive terminal");
        Console.WriteLine("  version              Display version information");
        Console.WriteLine();
        Console.WriteLine("Options (global):");
        Console.WriteLine("  --verbose, -v        Enable verbose logging");
        Console.WriteLine("  --no-color           Disable ANSI colors");
        Console.WriteLine("  --debug              Show stack traces on errors");
    }
}
