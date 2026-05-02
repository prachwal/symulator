using CmosCpu.Terminal.Session;
using CmosCpu.Terminal.Tui;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace CmosCpu.Terminal;

public static class Program
{
    public static int Main(string[] args)
    {
        ConfigureNLog(args);

        bool noColor = args.Any(a => a.Equals("--no-color", StringComparison.OrdinalIgnoreCase));
        if (noColor)
            Environment.SetEnvironmentVariable("NO_COLOR", "1");

        var session = new ComputerSession();
        var app = new TerminalApp(session);

        try
        {
            return app.Run(args);
        }
        catch (Exception ex)
        {
            bool debug = args.Any(a => a.Equals("--debug", StringComparison.OrdinalIgnoreCase));
            if (debug)
                Console.Error.WriteLine(ex);
            else
                Console.Error.WriteLine($"Fatal error: {ex.Message}");

            return 3;
        }
    }

    private static void ConfigureNLog(string[] args)
    {
        bool verbose = args.Any(a => a is "--verbose" or "-v");

        var config = new LoggingConfiguration();
        var fileTarget = new FileTarget("file")
        {
            FileName = "logs/simulator-terminal.log",
            Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message}",
            ArchiveEvery = FileArchivePeriod.Day,
            MaxArchiveFiles = 7,
            ArchiveAboveSize = 10485760
        };
        config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);

        if (verbose)
        {
            var consoleTarget = new ConsoleTarget("console")
            {
                Layout = "${message}"
            };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
        }

        LogManager.Configuration = config;
    }
}
