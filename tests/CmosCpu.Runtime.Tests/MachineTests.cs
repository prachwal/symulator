using CmosCpu.Bus;
using CmosCpu.Core;
using CmosCpu.Cpu;
using CmosCpu.Memory;
using CmosCpu.Runtime;
using FluentAssertions;

namespace CmosCpu.Runtime.Tests;

[TestClass]
public sealed class MachineBuilderTests
{
    [TestMethod]
    public void Builder_NoCpu_Throws()
    {
        var builder = new MachineBuilder();
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Builder_DoubleCpu_Throws()
    {
        var builder = new MachineBuilder();
        var cpu = new FakeCpu();

        builder.WithCpu(cpu);
        Action act = () => builder.WithCpu(new FakeCpu());
        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Builder_AddsDeviceToBus()
    {
        var bus = new SystemBus();
        var builder = new MachineBuilder(bus);
        var cpu = new FakeCpu();
        var rom = new RomDevice(0x8000, 0xBFFF);

        builder.WithCpu(cpu);
        builder.WithDevice(rom);
        var machine = builder.Build() as Machine;

        machine.Should().NotBeNull();
        byte val = bus.Read(0x8000);
        val.Should().Be(0);
    }

    [TestMethod]
    public void Machine_ResetResetsCpu()
    {
        var cpu = new FakeCpu();
        var bus = new SystemBus();
        var builder = new MachineBuilder(bus);

        builder.WithCpu(cpu);
        var machine = builder.Build();

        cpu.ResetCount = 0;
        machine.Reset();
        cpu.ResetCount.Should().Be(1);
    }

    [TestMethod]
    public void Machine_StepInstruction_DelegatesToCpu()
    {
        var cpu = new FakeCpu();
        var bus = new SystemBus();
        var builder = new MachineBuilder(bus);

        builder.WithCpu(cpu);
        var machine = builder.Build();

        machine.StepInstruction();
        cpu.StepCount.Should().Be(1);
    }

    [TestMethod]
    public void Machine_TicksClockedDevices()
    {
        var cpu = new FakeCpu();
        var clocked = new FakeClockedDevice();
        var bus = new SystemBus();
        var builder = new MachineBuilder(bus);

        builder.WithCpu(cpu);
        builder.WithClockedDevice(clocked);
        var machine = builder.Build();

        machine.StepCycle();
        clocked.TickCount.Should().Be(1);
        machine.Cycle.Should().Be(1);
    }

    [TestMethod]
    public void Machine_RunEndsAfterMaxCycles()
    {
        var cpu = new FakeCpu();
        var bus = new SystemBus();
        var builder = new MachineBuilder(bus);

        builder.WithCpu(cpu);
        var machine = builder.Build();

        machine.Run(5);
        machine.Cycle.Should().Be(5);
    }

    [TestMethod]
    public void Machine_RunEndsWhenCpuHalted()
    {
        var cpu = new FakeCpu { HaltedAfterSteps = 3 };
        var bus = new SystemBus();
        var builder = new MachineBuilder(bus);

        builder.WithCpu(cpu);
        var machine = builder.Build();

        machine.Run(100);
        machine.Cycle.Should().Be(3);
    }
}

[TestClass]
public sealed class FakeCpuTests
{
    [TestMethod]
    public void FakeCpu_Implements_ICpuCore()
    {
        var cpu = new FakeCpu();
        cpu.Should().BeAssignableTo<ICpuCore>();
        cpu.Should().BeAssignableTo<IResettable>();
        cpu.Should().BeAssignableTo<IClockedDevice>();
    }

    [TestMethod]
    public void FakeClockedDevice_Implements_IClockedDevice()
    {
        var device = new FakeClockedDevice();
        device.Should().BeAssignableTo<IClockedDevice>();
    }
}

[TestClass]
public sealed class CmosCpuCoreAdapterTests
{
    [TestMethod]
    public void Adapter_ReturnsName()
    {
        var bus = new SystemBus();
        var cpu = new Cpu.CpuCore(bus);
        var adapter = new CmosCpuCoreAdapter(cpu);

        adapter.Name.Should().Be("Educational CMOS 8-bit CPU");
    }

    [TestMethod]
    public void Adapter_Reset_DelegatesToCpu()
    {
        var bus = new SystemBus();
        var vectors = new RamDevice(0xFF00, 0xFFFF);
        bus.AttachDevice(vectors);
        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);
        var cpu = new Cpu.CpuCore(bus);
        var adapter = new CmosCpuCoreAdapter(cpu);

        adapter.Reset();
        adapter.Registers.PC.Should().Be(0x8000);
    }

    [TestMethod]
    public void Adapter_StepInstruction_DelegatesToCpu()
    {
        var bus = new SystemBus();
        var rom = new RomDevice(0x8000, 0xBFFF);
        var vectors = new RamDevice(0xFF00, 0xFFFF);
        bus.AttachDevice(rom);
        bus.AttachDevice(vectors);
        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);
        var cpu = new Cpu.CpuCore(bus);
        var adapter = new CmosCpuCoreAdapter(cpu);

        adapter.Reset();
        adapter.StepInstruction();
        adapter.Registers.PC.Should().Be(0x8001);
    }

    [TestMethod]
    public void Adapter_RequestInterrupt_Reset_Delegates()
    {
        var bus = new SystemBus();
        var vectors = new RamDevice(0xFF00, 0xFFFF);
        bus.AttachDevice(vectors);
        vectors.Write(0xFFFC, 0x00);
        vectors.Write(0xFFFD, 0x80);
        var cpu = new Cpu.CpuCore(bus);
        var adapter = new CmosCpuCoreAdapter(cpu);

        adapter.Reset();
        adapter.StepInstruction();
        adapter.RequestInterrupt(InterruptType.Reset);
        adapter.Registers.PC.Should().Be(0x8000);
    }

    [TestMethod]
    public void Adapter_RequestInterrupt_Unknown_Throws()
    {
        var bus = new SystemBus();
        var cpu = new Cpu.CpuCore(bus);
        var adapter = new CmosCpuCoreAdapter(cpu);

        Action act = () => adapter.RequestInterrupt((InterruptType)999);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

[TestClass]
public sealed class Educational8BitMachineProfileTests
{
    [TestMethod]
    public void Profile_CreatesMachineWithCpu()
    {
        var profile = new Educational8BitMachineProfile();
        var builder = new MachineBuilder();

        profile.Configure(builder);
        var machine = builder.Build();

        machine.Cpu.Should().NotBeNull();
        machine.Cpu.Name.Should().Be("Educational CMOS 8-bit CPU");
    }

    [TestMethod]
    public void Profile_CreatesMachineWithBus()
    {
        var profile = new Educational8BitMachineProfile();
        var builder = new MachineBuilder();

        profile.Configure(builder);
        var machine = builder.Build();

        machine.Bus.Should().NotBeNull();
    }

    [TestMethod]
    public void Profile_CanStepProgram()
    {
        var profile = new Educational8BitMachineProfile();
        var builder = new MachineBuilder();

        profile.Configure(builder);
        var machine = builder.Build();

        machine.Reset();
        machine.StepInstruction();
        machine.Cpu.Registers.PC.Should().Be(0x8001);
    }
}
