using CmosCpu.Bus;
using CmosCpu.Core;
using CmosCpu.Cpu;
using CmosCpu.Devices;
using CmosCpu.Memory;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CmosCpu.Cpu.Tests;

[TestClass]
public class IntegrationTests
{
    private static (SystemBus bus, RamDevice ram, RamDevice vectors, RomDevice rom, CpuCore cpu) CreateSystem()
    {
        var bus = new SystemBus();
        var ram = new RamDevice(0x0000, 0x7FFF);
        var vectors = new RamDevice(0xFF00, 0xFFFF);
        var rom = new RomDevice(0x8000, 0xBFFF);

        bus.AttachDevice(ram);
        bus.AttachDevice(vectors);
        bus.AttachDevice(rom);

        return (bus, ram, vectors, rom, new CpuCore(bus));
    }

    [TestMethod]
    public void BlinkProgram_TogglesLed()
    {
        var (bus, ram, vectors, rom, cpu) = CreateSystem();
        var led = new LedDevice();
        bus.AttachDevice(led);

        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);

        byte[] program =
        {
            0x01, 0x01,       // 0x8000: LDA #0x01
            0x03, 0x00, 0xC0, // 0x8002: STA 0xC000
            0x0F, 0x13, 0x80, // 0x8005: CALL 0x8013
            0x01, 0x00,       // 0x8008: LDA #0x00
            0x03, 0x00, 0xC0, // 0x800A: STA 0xC000
            0x0F, 0x13, 0x80, // 0x800D: CALL 0x8013
            0x06, 0x00, 0x80, // 0x8010: JMP 0x8000
            0x01, 0xFF,       // 0x8013: LDA #0xFF (delay)
            0x05, 0x01,       // 0x8015: SUB #0x01
            0x08, 0x15, 0x80, // 0x8017: JNZ 0x8015
            0x10,             // 0x801A: RET
        };

        rom.Load(program);

        int ledChanges = 0;
        led.StateChanged += (s, e) => ledChanges++;

        cpu.Reset();
        cpu.Registers.PC.Should().Be(0x8000);

        cpu.Run(5000);
        ledChanges.Should().BeGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public void Cpu_Bus_Memory_Integration_LdaSta()
    {
        var (bus, ram, vectors, rom, cpu) = CreateSystem();

        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);

        byte[] program =
        {
            0x01, 0x42,       // LDA #0x42
            0x03, 0x00, 0x10, // STA 0x1000
            0x11,             // HLT
        };

        rom.Load(program);

        cpu.Reset();
        cpu.Run(100);

        cpu.Registers.A.Should().Be(0x42);
        ram.Read(0x1000).Should().Be(0x42);
        cpu.Registers.Halted.Should().BeTrue();
    }

    [TestMethod]
    public void Cpu_RequestInterrupt_TriggersIrq()
    {
        var (bus, ram, vectors, rom, cpu) = CreateSystem();

        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);
        vectors.Write(0xFFFA, 0x1D);
        vectors.Write(0xFFFB, 0x80);

        byte[] program =
        {
            0x0B,             // 0x8000: CLI (enable interrupts)
            0x01, 0x55,       // 0x8001: LDA #0x55
            0x11,             // 0x8003: HLT
            0x00, 0x00,       // padding
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x00, 0x00,
            0x01, 0xAA,       // 0x801D: LDA #0xAA (IRQ handler)
            0x11,             // 0x801F: HLT
        };

        rom.Load(program);

        var cpu2 = cpu;
        cpu2.Reset();

        cpu2.Step();
        cpu2.Registers.InterruptDisableFlag.Should().BeFalse();

        cpu2.RequestInterrupt();
        cpu2.Step();

        cpu2.Registers.PC.Should().Be(0x801D);

        cpu2.Run(100);
        cpu2.Registers.A.Should().Be(0xAA);
        cpu2.Registers.Halted.Should().BeTrue();
    }

    [TestMethod]
    public void HltInstruction_HaltsCPU()
    {
        var (bus, ram, vectors, rom, cpu) = CreateSystem();

        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);

        byte[] program = { 0x11 };
        rom.Load(program);

        cpu.Reset();
        cpu.Step();
        cpu.Registers.Halted.Should().BeTrue();
    }

    [TestMethod]
    public void AddSub_Flags_WorkCorrectly()
    {
        var (bus, ram, vectors, rom, cpu) = CreateSystem();

        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);

        byte[] program =
        {
            0x01, 0x80,       // LDA #0x80
            0x04, 0x80,       // ADD #0x80 (0x80+0x80=0x100 -> A=0, Carry=1)
            0x01, 0x50,       // LDA #0x50
            0x05, 0x30,       // SUB #0x30 (0x50-0x30=0x20)
            0x11,             // HLT
        };

        rom.Load(program);

        cpu.Reset();
        cpu.Run(100);

        cpu.Registers.A.Should().Be(0x20);
        cpu.Registers.CarryFlag.Should().BeTrue();
    }

    [TestMethod]
    public void PushPop_Stack_Works()
    {
        var (bus, ram, vectors, rom, cpu) = CreateSystem();

        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);

        byte[] program =
        {
            0x01, 0x42,       // LDA #0x42
            0x0D,             // PUSH_A
            0x01, 0x00,       // LDA #0x00
            0x0E,             // POP_A
            0x11,             // HLT
        };

        rom.Load(program);

        cpu.Reset();
        cpu.Run(100);

        cpu.Registers.A.Should().Be(0x42);
        cpu.Registers.Halted.Should().BeTrue();
    }
}