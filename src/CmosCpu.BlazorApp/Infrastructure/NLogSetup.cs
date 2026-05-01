using NLog;
using NLog.Config;
using NLog.Targets;

namespace CmosCpu.BlazorApp.Infrastructure;

public static class NLogSetup
{
    public static void Configure()
    {
        var config = new LoggingConfiguration();
        var consoleTarget = new ConsoleTarget("console")
        {
            Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message}"
        };
        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, consoleTarget);
        LogManager.Configuration = config;
    }
}
