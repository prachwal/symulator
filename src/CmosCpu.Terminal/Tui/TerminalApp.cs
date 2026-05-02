using CmosCpu.Terminal.Commands;
using CmosCpu.Terminal.Session;
using NLog;

namespace CmosCpu.Terminal.Tui;

public sealed class TerminalApp
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ISimulatorSession _session;
    private readonly CommandRouter _router;
    private readonly TerminalScreenFactory _factory = new();

    public TerminalApp(ISimulatorSession session)
    {
        _session = session;
        _router = BuildRouter();
    }

    public int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintBanner();
            _router.PrintHelp();
            return 0;
        }

        string command = args[0];

        if (string.Equals(command, "interactive", StringComparison.OrdinalIgnoreCase))
        {
            var shell = new InteractiveShell(_session);
            shell.Run();
            return 0;
        }

        return _router.Execute(args);
    }

    private CommandRouter BuildRouter()
    {
        var router = new CommandRouter();
        var s = _session;

        router.Register("help", args => BatchCommands.HandleHelp(args));
        router.Register("version", args => BatchCommands.HandleVersion(args));
        router.Register("state", args => BatchCommands.HandleState(s, args));
        router.Register("profile", args => BatchCommands.HandleProfile(s, args));
        router.Register("reset", args => BatchCommands.HandleReset(s, args));
        router.Register("step", args => BatchCommands.HandleStep(s, args));
        router.Register("run", args => BatchCommands.HandleRun(s, args));
        router.Register("pause", args => BatchCommands.HandlePause(s, args));
        router.Register("registers", args => BatchCommands.HandleRegisters(s, args));
        router.Register("memory", args => BatchCommands.HandleMemory(s, args));
        router.Register("stack", args => BatchCommands.HandleStack(s, args));
        router.Register("terminal", args => BatchCommands.HandleTerminal(s, args));
        router.Register("load", args => BatchCommands.HandleLoad(s, args));
        router.Register("load-rom", args => BatchCommands.HandleLoadRom(s, args));
        router.Register("save", args => BatchCommands.HandleSave(s, args));
        router.Register("asm", args => BatchCommands.HandleAsm(s, args));
        router.Register("speed", args => BatchCommands.HandleSpeed(s, args));
        router.Register("breakpoints", args => BatchCommands.HandleBreakpoints(s, args));
        router.Register("kim1-io", args => BatchCommands.HandleKim1Io(s, args));
        router.Register("kim1", args => HandleTui("kim-1", s, args));
        router.Register("apple1-basic", args => BatchCommands.HandleApple1Basic(s, args));
        router.Register("apple1", args => BatchCommands.HandleApple1Basic(s, args));
        router.Register("apple1-tui", args => HandleTui("apple-1", s, args));

        router.RegisterAlias("?", "help");
        router.RegisterAlias("-h", "help");
        router.RegisterAlias("reg", "registers");
        router.RegisterAlias("mem", "memory");
        router.RegisterAlias("st", "stack");
        router.RegisterAlias("term", "terminal");
        router.RegisterAlias("loadbin", "load");
        router.RegisterAlias("loadrom", "load-rom");
        router.RegisterAlias("dump", "save");

        return router;
    }

    private int HandleTui(string platformId, ISimulatorSession session, string[] args)
    {
        string? profilePath = GetArgValue(args, "--profile")
            ?? (platformId == "kim-1" ? "profiles/kim-1.json" : "profiles/apple-1.json");

        if (!TerminalScreenFactory.LoadProfileForTui(session, profilePath))
        {
            Console.Error.WriteLine($"Profile not found: {profilePath}");
            return 1;
        }

        if (!_factory.Supports(platformId))
        {
            Console.Error.WriteLine($"No TUI screen for platform: {platformId}");
            return 1;
        }

        var screen = _factory.Create(platformId, args);
        screen.Run(session);
        return 0;
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

    private static void PrintBanner()
    {
        Console.WriteLine("Symulator Terminal Client v1.0.0");
        Console.WriteLine("Retro Computer Emulator - MOS 6502");
        Console.WriteLine();
    }
}
