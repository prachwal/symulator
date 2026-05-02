using CmosCpu.Computer;
using CmosCpu.Terminal.Commands;
using CmosCpu.Terminal.Session;
using CmosCpu.Terminal.Views;
using Spectre.Console;

namespace CmosCpu.Terminal.Tui;

public sealed class InteractiveShell
{
    private readonly ISimulatorSession _session;
    private long _lastScreenVersion = -1;
    private long _lastAppleTermVersion = -1;
    private bool _quit;

    public InteractiveShell(ISimulatorSession session)
    {
        _session = session;
    }

    public void Run()
    {
        if (!_session.IsLoaded)
        {
            AnsiConsole.MarkupLine("[yellow]No profile loaded. Loading default Retro70 profile...[/]");
            BatchCommands.HandleProfile(_session, []);
        }

        _quit = false;
        while (!_quit)
        {
            RenderDashboard();

            var key = Console.ReadKey(true);

            if (_session.IsRunning)
            {
                HandleRunKey(key);
            }
            else
            {
                HandleStopKey(key);
            }
        }
    }

    private void HandleRunKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Escape:
            case ConsoleKey.Spacebar:
                _session.Stop();
                break;

            case ConsoleKey.Q:
                _session.Stop();
                _quit = true;
                break;
        }
    }

    private void HandleStopKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.R:
                StartRun();
                break;

            case ConsoleKey.S:
                PerformStep();
                break;

            case ConsoleKey.P:
                LoadProfileMenu();
                break;

            case ConsoleKey.M:
                ShowMemoryViewer();
                break;

            case ConsoleKey.D:
                ShowRegisters();
                break;

            case ConsoleKey.T:
                ShowTerminalView();
                break;

            case ConsoleKey.K:
                SendKeyToTerminal();
                break;

            case ConsoleKey.L:
                LoadBinaryMenu();
                break;

            case ConsoleKey.H:
                ShowHelp();
                break;

            case ConsoleKey.Escape:
            case ConsoleKey.Q:
                _quit = true;
                break;
        }
    }

    private void StartRun()
    {
        if (_session.IsRunning) return;

        AnsiConsole.MarkupLine("[green]Running... Press SPACE or ESC to pause.[/]");
        _ = Task.Run(async () =>
        {
            try
            {
                await _session.StartAsync();
            }
            catch (OperationCanceledException) { }
        });
    }

    private void PerformStep()
    {
        if (_session.IsRunning || _session.Machine is null) return;

        if (_session.Machine.Cpu.IsHalted)
        {
            AnsiConsole.MarkupLine("[red]CPU is HALTED[/]");
            return;
        }

        int cycles = _session.Step();
        var cpu = _session.Machine.Cpu;
        AnsiConsole.MarkupLine($"Step: PC=0x{cpu.PC:X4} A=0x{cpu.A:X2} X=0x{cpu.X:X2} Y=0x{cpu.Y:X2} Cycles={cpu.CycleCount} (+{cycles})");
    }

    private void LoadProfileMenu()
    {
        string? path = AnsiConsole.Ask<string>("Profile JSON file path (or empty for default):");
        if (string.IsNullOrWhiteSpace(path))
        {
            BatchCommands.HandleProfile(_session, []);
        }
        else if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]File not found: {path}[/]");
        }
        else
        {
            _session.LoadProfileFromFile(path);
            AnsiConsole.MarkupLine($"[green]Loaded profile: {_session.Machine?.Profile.Name ?? path}[/]");
        }
    }

    private void ShowMemoryViewer()
    {
        if (_session.Machine is null) { AnsiConsole.MarkupLine("[red]No machine loaded[/]"); return; }

        string addrStr = AnsiConsole.Ask<string>("Start address:", "0x0000");
        if (!AddressParser.TryParse(addrStr, out ushort addr))
        {
            AnsiConsole.MarkupLine($"[red]Invalid address: {addrStr}[/]");
            return;
        }

        string lenStr = AnsiConsole.Ask<string>("Length:", "256");
        if (!int.TryParse(lenStr, out int len) || len <= 0) len = 256;

        len = Math.Min(len, 4096);
        byte[] data = _session.ReadMemory(addr, len);
        MemoryView.Render(data, addr);
        AnsiConsole.MarkupLine("[grey]Press any key to return...[/]");
        Console.ReadKey(true);
    }

    private void ShowRegisters()
    {
        if (_session.Machine is null) { AnsiConsole.MarkupLine("[red]No machine loaded[/]"); return; }
        RegistersView.Render(_session.Machine.Cpu);
        AnsiConsole.MarkupLine("[grey]Press any key to return...[/]");
        Console.ReadKey(true);
    }

    private void ShowTerminalView()
    {
        if (_session.Machine is null) { AnsiConsole.MarkupLine("[red]No machine loaded[/]"); return; }

        AnsiConsole.MarkupLine("[yellow]Terminal I/O[/]");
        if (_session.Machine.TextDisplay is not null)
            TerminalIoView.RenderTextDisplay(_session.Machine.TextDisplay);
        if (_session.Machine.Apple1Terminal is not null)
            TerminalIoView.RenderApple1Terminal(_session.Machine.Apple1Terminal);

        AnsiConsole.MarkupLine("[grey]Press any key to return...[/]");
        Console.ReadKey(true);
    }

    private void SendKeyToTerminal()
    {
        if (_session.Machine is null) { AnsiConsole.MarkupLine("[red]No machine loaded[/]"); return; }

        AnsiConsole.MarkupLine("[yellow]Type characters to send to the emulated machine. ESC to stop.[/]");
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape) break;

            if (_session.Machine.Keyboard is not null)
                _session.Machine.Keyboard.EnqueueKey(key.KeyChar);

            if (_session.Machine.Apple1Terminal is not null)
                _session.Machine.Apple1Terminal.QueueKey(key.KeyChar);
        }
    }

    private void LoadBinaryMenu()
    {
        string? file = AnsiConsole.Ask<string>("Binary file path:");
        if (!File.Exists(file))
        {
            AnsiConsole.MarkupLine($"[red]File not found: {file}[/]");
            return;
        }

        string addrStr = AnsiConsole.Ask<string>("Load address:", "0x0000");
        if (!AddressParser.TryParse(addrStr, out ushort addr))
        {
            AnsiConsole.MarkupLine($"[red]Invalid address: {addrStr}[/]");
            return;
        }

        byte[] data = File.ReadAllBytes(file);
        _session.LoadBinary(data, addr);
        AnsiConsole.MarkupLine($"[green]Loaded {data.Length} bytes at 0x{addr:X4}[/]");
    }

    private void ShowHelp()
    {
        AnsiConsole.MarkupLine(@"[yellow]Interactive Mode Key Bindings:[/]
  [green]R[/]     - Run emulation
  [green]S[/]     - Step one instruction
  [green]P[/]     - Load profile
  [green]M[/]     - Show memory viewer
  [green]D[/]     - Show registers/flags
  [green]T[/]     - Show terminal I/O
  [green]K[/]     - Send keyboard input to machine
  [green]L[/]     - Load binary file
  [green]H[/]     - Show this help
  [green]Q/ESC[/] - Quit

When running: [green]SPACE/ESC[/] pause, [green]Q[/] quit
");
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private void RenderDashboard()
    {
        Console.Clear();

        var machine = _session.Machine;
        if (machine is null)
        {
            AnsiConsole.MarkupLine("[yellow]No profile loaded. Press P to load a profile.[/]");
            return;
        }

        var cpu = machine.Cpu;

        string status = _session.IsRunning
            ? "[green]RUNNING[/]"
            : cpu.IsHalted
                ? "[red]HALTED[/]"
                : "[yellow]STOPPED[/]";

        var (batch, delay) = _session.GetSpeed();

        AnsiConsole.MarkupLine($"[bold]Symulator Terminal[/] | {status} | Profile: {machine.Profile.Name}");
        AnsiConsole.MarkupLine($"PC=0x{cpu.PC:X4} A=0x{cpu.A:X2} X=0x{cpu.X:X2} Y=0x{cpu.Y:X2} SP=0x{cpu.SP:X2} Cycles={cpu.CycleCount}");
        AnsiConsole.MarkupLine($"Flags: NV-BDIZC = {(cpu.Negative ? '1' : '0')}{(cpu.Overflow ? '1' : '0')}{(cpu.Break ? '1' : '0')}{(cpu.Decimal ? '1' : '0')}{(cpu.InterruptDisable ? '1' : '0')}{(cpu.Zero ? '1' : '0')}{(cpu.Carry ? '1' : '0')}");
        AnsiConsole.MarkupLine($"Instructions: {_session.TotalInstructionsExecuted} | Batch: {batch} | Delay: {delay}ms");

        if (!string.IsNullOrEmpty(_session.LastError))
            AnsiConsole.MarkupLine($"[red]Error: {_session.LastError}[/]");

        // Show terminal screen if available
        if (machine.TextDisplay is not null && machine.TextDisplay.Version != _lastScreenVersion)
        {
            _lastScreenVersion = machine.TextDisplay.Version;
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[underline]Text Screen:[/]");
            string screenText = TerminalIoView.GetScreenText(machine.TextDisplay);
            foreach (string line in screenText.Split(Environment.NewLine))
                AnsiConsole.MarkupLine($"[grey]|[/]{line.EscapeMarkup()}");
        }

        if (machine.Apple1Terminal is not null && machine.Apple1Terminal.Version != _lastAppleTermVersion)
        {
            _lastAppleTermVersion = machine.Apple1Terminal.Version;
            if (!string.IsNullOrEmpty(machine.Apple1Terminal.Text))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[underline]Apple-1 Terminal:[/]");
                var lines = machine.Apple1Terminal.Text.Split('\n');
                foreach (var line in lines.TakeLast(5))
                    AnsiConsole.MarkupLine($"[grey]|[/]{line.EscapeMarkup()}");
            }
        }

        // Status bar
        AnsiConsole.WriteLine();
        if (_session.IsRunning)
            AnsiConsole.MarkupLine("[grey]SPACE=pause  Q=quit[/]");
        else
            AnsiConsole.MarkupLine("[grey]R=run  S=step  M=memory  D=regs  T=terminal  K=keyboard  P=profile  L=load  H=help  Q=quit[/]");
    }
}
