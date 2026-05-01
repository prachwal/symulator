using CmosCpu.Assembler;
using CmosCpu.Bus;
using CmosCpu.Core;
using CmosCpu.Cpu;
using CmosCpu.Devices;
using CmosCpu.Memory;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CmosCpu.Runtime;

public class Simulator : ISimulator
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly SystemBus _bus;
    private readonly CpuCore _cpu;
    private readonly RamDevice _ram;
    private readonly RomDevice _rom;
    private readonly RamDevice _vectors;
    private readonly LedDevice _led;
    private readonly TimerDevice? _timer;
    private readonly SimpleAssembler _assembler = new();
    private readonly ServiceProvider _serviceProvider;
    private volatile bool _paused;
    private volatile bool _stopped;

    public event EventHandler<SimulatorSnapshot>? SnapshotChanged;
    public event EventHandler<TraceEventArgs>? TraceExecuted;

    public SystemBus Bus => _bus;
    public CpuCore Cpu => _cpu;
    public RamDevice Ram => _ram;
    public RomDevice Rom => _rom;
    public RamDevice Vectors => _vectors;
    public LedDevice Led => _led;
    public TimerDevice? Timer => _timer;
    public LedDevice LedDevice => _led;

    public Simulator(ushort romStart = 0x8000, ushort romEnd = 0xBFFF, ushort ramStart = 0x0000, ushort ramEnd = 0x7FFF, bool enableTimer = false)
    {
        var services = new ServiceCollection();
        _bus = new SystemBus();
        _ram = new RamDevice(ramStart, ramEnd);
        _rom = new RomDevice(romStart, romEnd);
        _vectors = new RamDevice(0xFF00, 0xFFFF);
        _led = new LedDevice();
        if (enableTimer)
            _timer = new TimerDevice();

        _bus.AttachDevice(_ram);
        _bus.AttachDevice(_rom);
        _bus.AttachDevice(_vectors);
        _bus.AttachDevice(_led);

        _vectors.Write(0xFFFC, 0x00);
        _vectors.Write(0xFFFD, 0x80);

        _cpu = new CpuCore(_bus);

        if (_timer != null)
        {
            _bus.AttachDevice(_timer);
        }

        services.AddSingleton<IBus>(_bus);
        services.AddSingleton(_cpu);
        services.AddSingleton(_ram);
        services.AddSingleton(_rom);
        services.AddSingleton(_led);
        services.AddSingleton<ISimulator>(this);

        _serviceProvider = services.BuildServiceProvider();
    }

    public bool LoadProgram(string source)
    {
        var assembler = new SimpleAssembler();
        var (binary, errors) = assembler.Assemble(source, 0x8000);

        if (errors.Count > 0)
        {
            Logger.Error("Assembly errors:");
            foreach (var err in errors)
                Logger.Error("  {Error}", err);
            return false;
        }

        _rom.Load(binary);
        Logger.Info("Program loaded: {Bytes} bytes at ROM", binary.Length);
        return true;
    }

    public bool LoadBinary(byte[] binary)
    {
        _rom.Load(binary);
        Logger.Info("Binary loaded: {Bytes} bytes at ROM", binary.Length);
        return true;
    }

    public void Reset()
    {
        _cpu.Reset();
    }

    public void Step()
    {
        if (_cpu.Halted) return;
        ulong before = _cpu.CycleCount;
        _cpu.Step();
        _timer?.Tick();
        EmitTrace(before);
        EmitSnapshot();
    }

    public void Run(int maxCycles)
    {
        _paused = false;
        _stopped = false;
        ulong target = _cpu.CycleCount + (ulong)maxCycles;
        while (_cpu.CycleCount < target && !_cpu.Halted && !_paused && !_stopped)
        {
            ulong before = _cpu.CycleCount;
            _cpu.Step();
            _timer?.Tick();
            EmitTrace(before);
        }
        EmitSnapshot();
    }

    public void Pause()
    {
        _paused = true;
    }

    public void Stop()
    {
        _stopped = true;
        _paused = false;
    }

    public SimulatorSnapshot GetSnapshot()
    {
        var regs = _cpu.Registers.Clone();
        var dump = new Dictionary<ushort, byte>();
        byte[] ramDump = _ram.Dump(0x0000, 0x00FF);
        for (int i = 0; i < ramDump.Length; i++)
            dump[(ushort)i] = ramDump[i];

        var memWindow = new List<MemoryCellSnapshot>();
        ushort startAddr = (ushort)(regs.PC > 0x20 ? regs.PC - 0x20 : 0);
        for (ushort addr = startAddr; addr < startAddr + 256 && addr < 0xFFFF; addr++)
        {
            byte val = _bus.Read(addr);
            memWindow.Add(new MemoryCellSnapshot
            {
                Address = addr,
                Value = val,
                IsPC = addr == regs.PC,
            });
        }

        TimerSnapshot? timerSnap = null;
        if (_timer != null)
        {
            timerSnap = new TimerSnapshot
            {
                Counter = (ushort)(_timer.Read(0xC010)),
                Control = _timer.Read(0xC011),
                Status = _timer.Read(0xC012),
                Running = (_timer.Read(0xC011) & 0x80) != 0,
                IrqEnabled = _timer.IrqEnabled,
            };
        }

        var interruptSnap = new InterruptSnapshot
        {
            InterruptDisable = regs.InterruptDisableFlag,
            ResetVector = (ushort)((_bus.Read(0xFFFD) << 8) | _bus.Read(0xFFFC)),
            IrqVector = (ushort)((_bus.Read(0xFFFB) << 8) | _bus.Read(0xFFFA)),
            NmiVector = (ushort)((_bus.Read(0xFFFF) << 8) | _bus.Read(0xFFFE)),
        };

        return new SimulatorSnapshot
        {
            Registers = regs,
            CycleCount = _cpu.CycleCount,
            LedOn = _led.IsOn,
            MemoryDump = dump,
            MemoryWindow = memWindow.AsReadOnly(),
            CurrentInstruction = GetCurrentInstruction(),
            Timer = timerSnap,
            Interrupts = interruptSnap,
        };
    }

    private string? GetCurrentInstruction()
    {
        if (_cpu.Halted) return "HLT";
        byte opcode = _bus.Read(_cpu.Registers.PC);
        if (InstructionInfo.All.TryGetValue((Opcode)opcode, out var info))
            return info.Mnemonic;
        return $"?? ({opcode:X2})";
    }

    private void EmitTrace(ulong beforeCycle)
    {
        var regs = _cpu.Registers;
        byte opcode = _bus.Read((ushort)(regs.PC - 1));
        string mnemonic = "???";
        string desc = "";
        if (InstructionInfo.All.TryGetValue((Opcode)opcode, out var info))
        {
            mnemonic = info.Mnemonic;
            desc = info.Description;
        }

        TraceExecuted?.Invoke(this, new TraceEventArgs
        {
            Entry = new TraceEntry
            {
                Cycle = beforeCycle,
                PC = (ushort)(regs.PC - 1),
                Opcode = opcode,
                Mnemonic = mnemonic,
                A = regs.A,
                X = regs.X,
                Y = regs.Y,
                SP = (ushort)(0x0100 + regs.SP),
                Flags = regs.Flags.ToString(),
                Description = desc,
                IsHalted = regs.Halted,
            }
        });
    }

    private void EmitSnapshot()
    {
        SnapshotChanged?.Invoke(this, GetSnapshot());
    }
}
