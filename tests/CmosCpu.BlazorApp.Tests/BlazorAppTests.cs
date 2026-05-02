using CmosCpu.BlazorApp.Models;
using CmosCpu.BlazorApp.Services;
using CmosCpu.Core;
using CmosCpu.Runtime;
using FluentAssertions;
using Moq;

namespace CmosCpu.BlazorApp.Tests;

[TestClass]
public class BlazorAppTests
{
    [TestMethod]
    public void BlazorSimulationController_Step_CallsSimulatorStep()
    {
        var simulator = new Simulator();
        var controller = new BlazorSimulationController(simulator);

        simulator.Rom.Load(new byte[] { 0x00, 0x11 });
        controller.Reset();

        controller.Step();

        simulator.Cpu.CycleCount.Should().Be(1);
        simulator.Cpu.Registers.PC.Should().Be(0x8001);
    }

    [TestMethod]
    public void BlazorSimulationController_Reset_CallsSimulatorReset()
    {
        var simulator = new Simulator();
        var controller = new BlazorSimulationController(simulator);

        simulator.Vectors.Write(0xFFFC, 0x00);
        simulator.Vectors.Write(0xFFFD, 0x80);
        simulator.Rom.Load(new byte[] { 0x11 });
        simulator.Reset();
        controller.Step();
        simulator.Cpu.Registers.Halted.Should().BeTrue();

        controller.Reset();

        simulator.Cpu.Registers.Halted.Should().BeFalse();
        simulator.Cpu.Registers.PC.Should().Be(0x8000);
    }

    [TestMethod]
    public async Task BlazorSimulationController_Pause_StopsRunning()
    {
        var simulator = new Simulator();
        var controller = new BlazorSimulationController(simulator);

        simulator.Rom.Load(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 });
        controller.Reset();

        controller.SpeedHz = 1000;
        var cts = new CancellationTokenSource();
        var task = controller.RunAsync(cts.Token);

        await Task.Delay(30);
        controller.Pause();

        await Task.Delay(50);
        controller.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public async Task BlazorSimulationController_Stop_StopsRunning()
    {
        var simulator = new Simulator();
        var controller = new BlazorSimulationController(simulator);

        simulator.Rom.Load(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 });
        controller.Reset();

        controller.SpeedHz = 1000;
        var cts = new CancellationTokenSource();
        var task = controller.RunAsync(cts.Token);

        await Task.Delay(30);
        controller.Stop();

        await Task.Delay(50);
        controller.IsRunning.Should().BeFalse();
    }

    [TestMethod]
    public void UiSimulatorState_UpdateFromSnapshot_SetsAllProperties()
    {
        var state = new UiSimulatorState();
        var snap = CreateSnapshot(regs =>
        {
            regs.A = 0x42;
            regs.X = 0xAB;
            regs.PC = 0x8000;
            regs.ZeroFlag = true;
            regs.InterruptDisableFlag = true;
        });

        state.UpdateFromSnapshot(snap);

        state.A.Should().Be(0x42);
        state.X.Should().Be(0xAB);
        state.PC.Should().Be(0x8000);
        state.Zero.Should().BeTrue();
        state.InterruptDisable.Should().BeTrue();
        state.MemoryRows.Should().NotBeEmpty();
    }

    [TestMethod]
    public void MemoryRowViewModel_Format_HexCorrect()
    {
        var row = new MemoryRowViewModel { Address = 0x8000 };
        row.SetCell(0, 0xAB, true);
        row.SetCell(1, 0xCD, false);

        row.Cells[0].Should().Be("0xAB");
        row.Cells[1].Should().Be("0xCD");
        row.IsPc[0].Should().BeTrue();
        row.IsPc[1].Should().BeFalse();
    }

    [TestMethod]
    public void TraceEntryViewModel_FromEntry_FormatsCorrectly()
    {
        var entry = new TraceEntry
        {
            Cycle = 5,
            PC = 0x8000,
            Opcode = 0x01,
            Mnemonic = "LDA",
            A = 0x42,
            X = 0x00,
            Y = 0x00,
            SP = 0x01FF,
            Flags = "Zero",
            Description = "Load A",
        };

        var vm = TraceEntryViewModel.FromEntry(entry);

        vm.Cycle.Should().Be("5");
        vm.PC.Should().Be("0x8000");
        vm.Opcode.Should().Be("0x01");
        vm.Mnemonic.Should().Be("LDA");
        vm.A.Should().Be("0x42");
        vm.SP.Should().Be("0x01FF");
        vm.Flags.Should().Be("Zero");
    }

    [TestMethod]
    public void BusTransactionViewModel_FromTransaction_FormatsCorrectly()
    {
        var tx = new BusTransaction
        {
            Cycle = 3,
            Operation = BusOperation.Write,
            Address = 0xC000,
            Value = 0x01,
            DeviceName = "LedDevice",
        };

        var vm = BusTransactionViewModel.FromTransaction(tx);

        vm.Cycle.Should().Be("3");
        vm.Operation.Should().Be("WRITE");
        vm.Address.Should().Be("0xC000");
        vm.Value.Should().Be("0x01");
        vm.Device.Should().Be("LedDevice");
    }

    [TestMethod]
    public void UiSimulatorState_LedState_UpdatesCorrectly()
    {
        var state = new UiSimulatorState();
        var snap = CreateSnapshot(ledOn: true);

        state.UpdateFromSnapshot(snap);

        state.LedOn.Should().BeTrue();

        state.UpdateFromSnapshot(CreateSnapshot(ledOn: false));

        state.LedOn.Should().BeFalse();
    }

    [TestMethod]
    public void ProgramUploadService_RejectsWrongExtension()
    {
        var service = new ProgramUploadService();

        service.ValidateFile("test.txt", 100, out var error).Should().BeFalse();
        error.Should().Contain(".txt");
    }

    [TestMethod]
    public void ProgramUploadService_AcceptsAsm()
    {
        var service = new ProgramUploadService();

        service.ValidateFile("program.asm", 100, out var error).Should().BeTrue();
    }

    [TestMethod]
    public void ProgramUploadService_RejectsOversizedFile()
    {
        var service = new ProgramUploadService();

        service.ValidateFile("program.asm", 2 * 1024 * 1024, out var error).Should().BeFalse();
        error.Should().Contain("large");
    }

    [TestMethod]
    public void CompileAsm_ReturnsOkForValidBlink()
    {
        var simulator = new Simulator();
        var controller = new BlazorSimulationController(simulator);

        string source = @"
.org 0x8000
start:
    LDA #0x01
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

        var result = controller.CompileAsm(source);

        result.Success.Should().BeTrue();
        result.ByteCount.Should().BeGreaterThan(0);
        result.HexDump.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void CompileAsm_ReturnsErrorsForBadLabel()
    {
        var simulator = new Simulator();
        var controller = new BlazorSimulationController(simulator);

        string source = @"
.org 0x8000
    JMP nonexistent_label
";

        var result = controller.CompileAsm(source);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [TestMethod]
    public void CompileLoadResetAsm_LoadsProgramIntoSimulator()
    {
        var simulator = new Simulator();
        var controller = new BlazorSimulationController(simulator);

        string source = @"
.org 0x8000
    LDA #0x42
    HLT
";

        var result = controller.CompileLoadResetAsm(source);

        result.Success.Should().BeTrue();
        simulator.Cpu.Registers.PC.Should().Be(0x8000);
        simulator.Cpu.Registers.A.Should().Be(0);
    }

    [TestMethod]
    public void CompileLoadResetAsm_StepExecutesLoadedProgram()
    {
        var simulator = new Simulator();
        var controller = new BlazorSimulationController(simulator);

        string source = @"
.org 0x8000
    LDA #0x42
    HLT
";

        controller.CompileLoadResetAsm(source);
        controller.Step();

        simulator.Cpu.Registers.A.Should().Be(0x42);
        simulator.Cpu.Registers.Halted.Should().BeFalse();
    }

    [TestMethod]
    public void MachineDi_CanResolveViaProfile()
    {
        var profile = new Educational8BitMachineProfile();
        var builder = new MachineBuilder();
        profile.Configure(builder);
        var machine = builder.Build();

        machine.Should().NotBeNull();
        machine.Cpu.Should().NotBeNull();
        machine.Cpu.Name.Should().Be("Educational CMOS 8-bit CPU");
    }

    // --- Helpers ---

    private static SimulatorSnapshot CreateSnapshot(Action<CpuRegisters>? configureRegs = null, bool ledOn = false)
    {
        var regs = new CpuRegisters();
        configureRegs?.Invoke(regs);

        return new SimulatorSnapshot
        {
            Registers = regs,
            CycleCount = regs.CycleCount,
            LedOn = ledOn,
            MemoryWindow = new List<MemoryCellSnapshot>
            {
                new() { Address = 0x8000, Value = 0x01, IsPC = true },
                new() { Address = 0x8001, Value = 0x42 },
            },
            Interrupts = new InterruptSnapshot
            {
                ResetVector = 0x8000,
                IrqVector = 0xFFFA,
                NmiVector = 0xFFFE,
            },
        };
    }
}
