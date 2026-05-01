using CmosCpu.Core;
using CmosCpu.WpfApp.Services;
using CmosCpu.WpfApp.ViewModels;
using FluentAssertions;
using Moq;

namespace CmosCpu.WpfApp.Tests;

[TestClass]
public class WpfAppTests
{
    [TestMethod]
    public void RegistersViewModel_Update_SetsAllProperties()
    {
        var vm = new RegistersViewModel();
        var snap = CreateSnapshot(regs =>
        {
            regs.A = 0x42;
            regs.X = 0xAB;
            regs.Y = 0xCD;
            regs.PC = 0x8000;
            regs.SP = 0xFD;
            regs.CycleCount = 100;
            regs.Halted = false;
        });

        vm.Update(snap);

        vm.A.Should().Be(0x42);
        vm.X.Should().Be(0xAB);
        vm.Y.Should().Be(0xCD);
        vm.PC.Should().Be(0x8000);
        vm.SP.Should().Be(0x01FD);
        vm.Cycles.Should().Be(100);
        vm.Halted.Should().BeFalse();
    }

    [TestMethod]
    public void FlagsViewModel_Update_SetsAllFlags()
    {
        var vm = new FlagsViewModel();
        var snap = CreateSnapshot(regs =>
        {
            regs.ZeroFlag = true;
            regs.CarryFlag = false;
            regs.NegativeFlag = true;
            regs.InterruptDisableFlag = false;
        });

        vm.Update(snap);

        vm.Zero.Should().BeTrue();
        vm.Carry.Should().BeFalse();
        vm.Negative.Should().BeTrue();
        vm.InterruptDisable.Should().BeFalse();
    }

    [TestMethod]
    public void LedViewModel_Update_TurnsOn()
    {
        var vm = new LedViewModel();
        var snap = CreateSnapshot(ledOn: true);

        vm.Update(snap);

        vm.IsOn.Should().BeTrue();
        vm.StateText.Should().Be("ON");
    }

    [TestMethod]
    public void LedViewModel_Update_TurnsOff()
    {
        var vm = new LedViewModel();
        vm.Update(CreateSnapshot(ledOn: true));

        vm.Update(CreateSnapshot(ledOn: false));

        vm.IsOn.Should().BeFalse();
        vm.StateText.Should().Be("OFF");
    }

    [TestMethod]
    public void TraceLogViewModel_AddEntry_AddsToCollection()
    {
        var vm = new TraceLogViewModel();
        vm.Entries.Should().BeEmpty();

        vm.AddEntry(new TraceEntry { Cycle = 1, Mnemonic = "LDA" });

        vm.Entries.Should().HaveCount(1);
        vm.Entries[0].Cycle.Should().Be(1);
    }

    [TestMethod]
    public void TraceLogViewModel_RespectsMaxLimit()
    {
        var vm = new TraceLogViewModel();

        for (int i = 0; i < 10001; i++)
            vm.AddEntry(new TraceEntry { Cycle = (ulong)i });

        vm.Entries.Count.Should().BeLessThanOrEqualTo(10000);
    }

    [TestMethod]
    public void TraceLogViewModel_Clear_EmptiesCollection()
    {
        var vm = new TraceLogViewModel();
        vm.AddEntry(new TraceEntry { Cycle = 1 });
        vm.Clear();

        vm.Entries.Should().BeEmpty();
    }

    [TestMethod]
    public void RegistersViewModel_Halted_ShowsCorrectState()
    {
        var vm = new RegistersViewModel();
        var snap = CreateSnapshot(regs => regs.Halted = true);

        vm.Update(snap);

        vm.Halted.Should().BeTrue();
    }

    [TestMethod]
    public void MainWindowViewModel_StepCommand_CallsControllerStep()
    {
        var controller = new Mock<IUiSimulationController>();
        var fileDialog = new Mock<IFileDialogService>();
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

        var vm = CreateMainVm(controller, fileDialog, dispatcher);

        vm.StepCommand.Execute(null);

        controller.Verify(c => c.Step(), Times.Once);
    }

    [TestMethod]
    public void MainWindowViewModel_ResetCommand_CallsControllerReset()
    {
        var controller = new Mock<IUiSimulationController>();
        var fileDialog = new Mock<IFileDialogService>();
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

        var vm = CreateMainVm(controller, fileDialog, dispatcher);

        vm.ResetCommand.Execute(null);

        controller.Verify(c => c.Reset(), Times.Once);
    }

    [TestMethod]
    public void MainWindowViewModel_PauseCommand_CallsControllerPause()
    {
        var controller = new Mock<IUiSimulationController>();
        var fileDialog = new Mock<IFileDialogService>();
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

        var vm = CreateMainVm(controller, fileDialog, dispatcher);

        vm.PauseCommand.Execute(null);

        controller.Verify(c => c.Pause(), Times.Once);
    }

    [TestMethod]
    public void MainWindowViewModel_StopCommand_CallsControllerStop()
    {
        var controller = new Mock<IUiSimulationController>();
        var fileDialog = new Mock<IFileDialogService>();
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

        var vm = CreateMainVm(controller, fileDialog, dispatcher);

        vm.StopCommand.Execute(null);

        controller.Verify(c => c.Stop(), Times.Once);
    }

    [TestMethod]
    public void MainWindowViewModel_LoadAsmCommand_OpensDialog()
    {
        var controller = new Mock<IUiSimulationController>();
        var fileDialog = new Mock<IFileDialogService>();
        var dispatcher = new Mock<IDispatcherService>();
        dispatcher.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());
        fileDialog.Setup(f => f.OpenFileDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("test.asm");
        controller.Setup(c => c.LoadAsm("test.asm", out It.Ref<IReadOnlyList<string>>.IsAny))
            .Returns(true);

        var vm = CreateMainVm(controller, fileDialog, dispatcher);

        vm.LoadAsmCommand.Execute(null);

        fileDialog.Verify(f => f.OpenFileDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        controller.Verify(c => c.Reset(), Times.Once);
    }

    [TestMethod]
    public void InterruptsViewModel_Update_SetsVectors()
    {
        var vm = new InterruptsViewModel();
        var snap = CreateSnapshot(regs => regs.InterruptDisableFlag = true, hasInterrupts: true);

        vm.Update(snap);

        vm.InterruptDisable.Should().BeTrue();
        vm.ResetVector.Should().Be("0x8000");
    }

    [TestMethod]
    public void MemoryViewModel_Update_CreatesLines()
    {
        var vm = new MemoryViewModel();
        var snap = CreateSnapshot();

        vm.Update(snap);

        vm.Lines.Should().NotBeEmpty();
    }

    // --- Helpers ---

    private static SimulatorSnapshot CreateSnapshot(
        Action<CpuRegisters>? configureRegs = null,
        bool ledOn = false,
        bool hasInterrupts = false)
    {
        var regs = new CpuRegisters();
        configureRegs?.Invoke(regs);

        return new SimulatorSnapshot
        {
            Registers = regs,
            CycleCount = regs.CycleCount,
            LedOn = ledOn,
            MemoryWindow = new List<MemoryCellSnapshot>(),
            Interrupts = hasInterrupts ? new InterruptSnapshot
            {
                ResetVector = 0x8000,
                IrqVector = 0xFFFA,
                NmiVector = 0xFFFE,
            } : null,
        };
    }

    private static MainWindowViewModel CreateMainVm(
        Mock<IUiSimulationController> controller,
        Mock<IFileDialogService> fileDialog,
        Mock<IDispatcherService> dispatcher)
    {
        return new MainWindowViewModel(
            controller.Object,
            fileDialog.Object,
            dispatcher.Object,
            new RegistersViewModel(),
            new FlagsViewModel(),
            new MemoryViewModel(),
            new LedViewModel(),
            new TraceLogViewModel(),
            new DevicesViewModel(),
            new InterruptsViewModel());
    }
}
