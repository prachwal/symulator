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
    private readonly ServiceProvider _serviceProvider;

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
        _cpu.Step();
        _timer?.Tick();
    }

    public void Run(int maxCycles)
    {
        ulong target = _cpu.CycleCount + (ulong)maxCycles;
        while (_cpu.CycleCount < target && !_cpu.Halted)
        {
            _cpu.Step();
            _timer?.Tick();
        }
    }

    public SimulatorSnapshot GetSnapshot()
    {
        var regs = _cpu.Registers.Clone();
        var dump = new Dictionary<ushort, byte>();
        byte[] ramDump = _ram.Dump(0x0000, 0x00FF);
        for (int i = 0; i < ramDump.Length; i++)
            dump[(ushort)i] = ramDump[i];

        return new SimulatorSnapshot
        {
            Registers = regs,
            CycleCount = _cpu.CycleCount,
            LedOn = _led.IsOn,
            MemoryDump = dump,
        };
    }
}
