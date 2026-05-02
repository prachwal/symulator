using CmosCpu.Core;
using CmosCpu.Devices;
using CmosCpu.Memory;

namespace CmosCpu.Runtime;

public sealed class Educational8BitMachineProfile : IMachineProfile
{
    public string Name => "Educational 8-bit machine";

    public void Configure(IMachineBuilder builder)
    {
        var cpu = new Cpu.CpuCore(builder.Bus);
        var adapter = new CmosCpuCoreAdapter(cpu);

        var ram = new RamDevice(0x0000, 0x7FFF);
        var rom = new RomDevice(0x8000, 0xBFFF);
        var vectors = new RamDevice(0xFF00, 0xFFFF);
        var led = new LedDevice();

        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);

        builder.WithCpu(adapter);
        builder.WithDevice(ram);
        builder.WithDevice(rom);
        builder.WithDevice(vectors);
        builder.WithDevice(led);
    }

    public IMemoryMap CreateMemoryMap()
    {
        return new MemoryMap(new[]
        {
            new MemoryRegion { Name = "RAM", Start = 0x0000, End = 0x7FFF, DeviceName = "RamDevice" },
            new MemoryRegion { Name = "I/O", Start = 0xC000, End = 0xC0FF, DeviceName = "IODevices" },
            new MemoryRegion { Name = "ROM", Start = 0x8000, End = 0xBFFF, DeviceName = "RomDevice" },
            new MemoryRegion { Name = "Vectors", Start = 0xFF00, End = 0xFFFF, DeviceName = "Vectors" },
        });
    }
}