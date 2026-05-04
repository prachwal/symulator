using CmosCpu.Computer;
using CmosCpu.Core;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class ComputerRunControllerTests
{
    private const string TestProfile = """
    {
        "id": "test",
        "name": "Test",
        "cpu": "mos6502",
        "clockHz": 1000000,
        "memory": {
            "ram": [{ "start": "0x0000", "size": "0x8000" }],
            "rom": [{ "start": "0xF000", "size": "0x1000" }],
            "vectors": { "reset": "0xF000", "nmi": "0xF100", "irq": "0xF200" }
        },
        "devices": [
            { "type": "text-display", "id": "screen", "start": "0xD000", "width": 40, "height": 25 },
            { "type": "keyboard", "id": "keyboard", "dataAddress": "0xD800", "statusAddress": "0xD801" }
        ]
    }
    """;

    [TestMethod]
    public void Constructor_DefaultSettings()
    {
        var ctrl = new ComputerRunController();

        ctrl.InstructionsPerBatch.Should().Be(1000);
        ctrl.RenderDelayMs.Should().Be(16);
        ctrl.IsRunning.Should().BeFalse();
        ctrl.TotalInstructionsExecuted.Should().Be(0);
    }

    [TestMethod]
    public void SetMachine_StopsPreviousRun()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(TestProfile);
        var ctrl = new ComputerRunController();

        ctrl.SetMachine(machine);

        ctrl.Machine.Should().Be(machine);
        ctrl.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void Stop_NoRun_DoesNotThrow()
    {
        var ctrl = new ComputerRunController();

        ctrl.Stop();

        ctrl.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void StartAsync_NoMachine_DoesNotRun()
    {
        var ctrl = new ComputerRunController();

        var task = ctrl.StartAsync();

        task.IsCompleted.Should().BeTrue();
        ctrl.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task StartAsync_ThenStop_StopsExecution()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(TestProfile);
        var rom = new byte[0x1000];
        // Infinite NOP + JMP loop so machine keeps running until Stop()
        rom[0x000] = 0x4C; // JMP $F000
        rom[0x001] = 0x00;
        rom[0x002] = 0xF0;
        rom[0xFFC] = 0x00;
        rom[0xFFD] = 0xF0;
        machine.Memory.LoadRom(0xF000, rom);
        machine.Reset();

        var ctrl = new ComputerRunController();
        ctrl.InstructionsPerBatch = 10;
        ctrl.RenderDelayMs = 1;
        ctrl.SetMachine(machine);

        var runTask = ctrl.StartAsync();
        await Task.Delay(50);
        ctrl.Stop();
        await runTask;

        ctrl.TotalInstructionsExecuted.Should().BeGreaterThan(0);
        ctrl.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void DoubleStart_DoesNotStartSecondLoop()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(TestProfile);
        var rom = new byte[0x1000];
        // Infinite NOP + JMP loop so machine keeps running until Stop()
        rom[0x000] = 0x4C; // JMP $F000
        rom[0x001] = 0x00;
        rom[0x002] = 0xF0;
        rom[0xFFC] = 0x00;
        rom[0xFFD] = 0xF0;
        machine.Memory.LoadRom(0xF000, rom);
        machine.Reset();

        var ctrl = new ComputerRunController();
        ctrl.InstructionsPerBatch = 10;
        ctrl.RenderDelayMs = 1;
        ctrl.SetMachine(machine);

        var task1 = ctrl.StartAsync();
        var task2 = ctrl.StartAsync();

        task2.IsCompleted.Should().BeTrue();
        task1.IsCompleted.Should().BeFalse();

        ctrl.Stop();
    }

    [TestMethod]
    public void SpeedSettings_UpdateCorrectly()
    {
        var ctrl = new ComputerRunController();

        ctrl.InstructionsPerBatch = 500;
        ctrl.RenderDelayMs = 50;

        ctrl.InstructionsPerBatch.Should().Be(500);
        ctrl.RenderDelayMs.Should().Be(50);
    }

    [TestMethod]
    public void SetMachine_ClearsTotalInstructions()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(TestProfile);
        var ctrl = new ComputerRunController();

        ctrl.SetMachine(machine);
        ctrl.SetMachine(null);

        ctrl.TotalInstructionsExecuted.Should().Be(0);
        ctrl.Machine.Should().BeNull();
    }

    [TestMethod]
    public void GetCycleCount_ReturnsNull_WhenNoMachine()
    {
        var ctrl = new ComputerRunController();

        ctrl.GetCycleCount().Should().BeNull();
    }

    [TestMethod]
    public async Task HaltedCpu_StopsRunLoop()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(TestProfile);
        var rom = new byte[0x1000];
        // Fill ROM with BRK (0x00) which halts
        Array.Fill(rom, (byte)0x00);
        rom[0xFFC] = 0x00;
        rom[0xFFD] = 0xF0;
        machine.Memory.LoadRom(0xF000, rom);
        machine.Reset();

        var ctrl = new ComputerRunController();
        ctrl.InstructionsPerBatch = 100;
        ctrl.RenderDelayMs = 1;
        ctrl.SetMachine(machine);

        await ctrl.StartAsync();

        machine.Cpu.IsHalted.Should().BeTrue();
        ctrl.IsRunning.Should().BeFalse();
        ctrl.TotalInstructionsExecuted.Should().BeGreaterThan(0);
    }
}

[TestClass]
public sealed class TextDisplayVersionTests
{
    [TestMethod]
    public void TextDisplayRegion_Version_IncrementsOnWrite()
    {
        var display = new TextDisplayRegion(0xD000, 40, 25);

        long v0 = display.Version;
        display.Write(0xD000, (byte)'A');
        display.Version.Should().Be(v0 + 1);

        display.Write(0xD000, (byte)'A');
        display.Version.Should().Be(v0 + 2);
    }

    [TestMethod]
    public void TextDisplayRegion_Version_IncrementsOnClear()
    {
        var display = new TextDisplayRegion(0xD000, 40, 25);

        long v0 = display.Version;
        display.Clear();
        display.Version.Should().BeGreaterThan(v0);
    }

    [TestMethod]
    public void TextDisplayRegion_Read_DoesNotChangeVersion()
    {
        var display = new TextDisplayRegion(0xD000, 40, 25);
        display.Write(0xD000, (byte)'X');

        long vAfterWrite = display.Version;
        display.Read(0xD000);
        display.Read(0xD001);

        display.Version.Should().Be(vAfterWrite);
    }
}

[TestClass]
public sealed class MemoryPageTests
{
    [TestMethod]
    public void GetMemoryPage_Returns256Bytes()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRam(0x0000, 0x1000);
        bus.WriteByte(0x0000, 0xAA);
        bus.WriteByte(0x00FF, 0xBB);

        var page = bus.GetMemoryPage(0x0000, 256);

        page.Length.Should().Be(256);
        page[0].Should().Be(0xAA);
        page[255].Should().Be(0xBB);
    }

    [TestMethod]
    public void GetMemoryPage_NonZeroStart()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRam(0x1000, 0x1000);
        bus.WriteByte(0x1000, 0x11);
        bus.WriteByte(0x10FF, 0xFF);

        var page = bus.GetMemoryPage(0x1000, 256);

        page.Length.Should().Be(256);
        page[0].Should().Be(0x11);
        page[255].Should().Be(0xFF);
    }

    [TestMethod]
    public void GetMemoryPage_UnmappedRegion_ReturnsOpenBusValue()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRam(0x0000, 0x1000);

        var page = bus.GetMemoryPage(0xF000, 256);

        page.Length.Should().Be(256);
        foreach (var b in page)
            b.Should().Be(bus.OpenBusValue);
    }
}
