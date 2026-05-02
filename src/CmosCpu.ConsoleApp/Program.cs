using CmosCpu.Runtime;

using NLog;
using NLog.Config;
using NLog.Targets;

namespace CmosCpu.ConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        var config = new LoggingConfiguration();
        var consoleTarget = new ConsoleTarget("console")
        {
            Layout = "${message}"
        };
        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, consoleTarget);
        LogManager.Configuration = config;

        string? programPath = null;
        int cycles = 10000;
        bool trace = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--program":
                    if (i + 1 < args.Length) programPath = args[++i];
                    break;
                case "--cycles":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int c)) cycles = c;
                    break;
                case "--trace":
                    trace = true;
                    break;
            }
        }

        if (trace)
        {
            var traceConfig = LogManager.Configuration ?? new LoggingConfiguration();
            var traceTarget = new ConsoleTarget("trace")
            {
                Layout = "${message}"
            };
            traceConfig.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Trace, traceTarget);
            LogManager.Configuration = traceConfig;
        }

        var simulator = new Simulator(enableTimer: false);
        string source;

        if (!string.IsNullOrEmpty(programPath) && File.Exists(programPath))
        {
            source = File.ReadAllText(programPath);
            Console.WriteLine($"Loading program from {programPath}");
        }
        else
        {
            source = GetDefaultBlinkProgram();
            Console.WriteLine("No program file specified, using default blink program");
        }

        if (!simulator.LoadProgram(source))
        {
            Console.WriteLine("Failed to load program");
            return;
        }

        simulator.Reset();

        simulator.LedDevice.StateChanged += (s, e) =>
        {
            Console.WriteLine($"Cycle {simulator.Cpu.CycleCount}: LED {(e.IsOn ? "ON" : "OFF")}");
        };

        Console.WriteLine($"Running for {cycles} cycles...");
        simulator.Run(cycles);

        var snapshot = simulator.GetSnapshot();
        Console.WriteLine();
        Console.WriteLine("=== Final State ===");
        Console.WriteLine($"Cycles: {snapshot.CycleCount}");
        Console.WriteLine($"A: 0x{snapshot.Registers.A:X2}");
        Console.WriteLine($"X: 0x{snapshot.Registers.X:X2}");
        Console.WriteLine($"Y: 0x{snapshot.Registers.Y:X2}");
        Console.WriteLine($"PC: 0x{snapshot.Registers.PC:X4}");
        Console.WriteLine($"SP: 0x{snapshot.Registers.SP:X2}");
        Console.WriteLine($"Flags: {snapshot.Registers.Flags}");
        Console.WriteLine($"LED: {(snapshot.LedOn ? "ON" : "OFF")}");
        Console.WriteLine($"Halted: {snapshot.Registers.Halted}");
    }

    static string GetDefaultBlinkProgram()
    {
        return @"
.org 0x8000

start:
    LDA #0x01
    STA 0xC000
    CALL delay
    LDA #0x00
    STA 0xC000
    CALL delay
    JMP start

delay:
    LDA #0xFF
loop:
    SUB #0x01
    JNZ loop
    RET
";
    }
}