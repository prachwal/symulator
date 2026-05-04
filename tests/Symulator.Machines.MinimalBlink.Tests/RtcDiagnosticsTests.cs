using CmosCpu.Computer.Devices.Rtc;
using Symulator.Application.Abstractions;
using Symulator.Application.Solutions;
using Symulator.Machines.MinimalBlink.Hardware;
using Symulator.Machines.MinimalBlink.Module;
using FluentAssertions;

namespace Symulator.Machines.MinimalBlink.Tests;

[TestClass]
public sealed class RtcDiagnosticsTests
{
    private static SolutionDefinition RtcI2cSolution => new()
    {
        Id = "rtc-test",
        Devices =
        [
            new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
            new DeviceDefinition { Id = "i2c0", Type = "i2c-controller-mmio", BaseAddress = "0xFE30", Visible = true },
            new DeviceDefinition { Id = "rtc0", Type = "rtc-i2c", Address = "0x68", Visible = true },
        ]
    };

    private static SolutionDefinition RtcBusSolution => new()
    {
        Id = "rtc-bus-test",
        Devices =
        [
            new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
            new DeviceDefinition { Id = "rtc0", Type = "rtc-mmio", BaseAddress = "0xD100", Visible = true },
        ]
    };

    [TestMethod]
    public void Build_RtcI2cSolution_RuntimeExposesRtcSnapshot()
    {
        var runtime = new MinimalBlinkHardwareSolutionBuilder().Build(RtcI2cSolution);

        runtime.RtcSnapshot.Should().NotBeNull();
        runtime.RtcSnapshot!.TimeMode.Should().Be(RtcTimeMode.Simulated);
        runtime.RtcSnapshot.I2cAddress.Should().Be(0x68);
        runtime.RtcSnapshot.DirectBusBaseAddress.Should().BeNull();
        runtime.RtcSnapshot.DirectBusMode.Should().BeNull();
        runtime.RtcSnapshot.Registers.Should().NotBeEmpty();
        runtime.RtcSnapshot.CurrentTime.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0));
        runtime.RtcI2c.Should().NotBeNull();
        runtime.RtcClock.Should().NotBeNull();
        runtime.RtcBus.Should().BeNull();
    }

    [TestMethod]
    public void Build_RtcBusSolution_RuntimeExposesDirectBusMetadata()
    {
        var runtime = new MinimalBlinkHardwareSolutionBuilder().Build(RtcBusSolution);

        runtime.RtcSnapshot.Should().NotBeNull();
        runtime.RtcSnapshot!.DirectBusBaseAddress.Should().Be(0xD100);
        runtime.RtcSnapshot.DirectBusMode.Should().Be(RtcBusMode.Linear);
        runtime.RtcSnapshot.I2cAddress.Should().BeNull();
        runtime.RtcBus.Should().NotBeNull();
        runtime.RtcI2c.Should().BeNull();
    }

    [TestMethod]
    public async Task SessionSnapshot_ForRtcI2cSolution_IncludesRtcData()
    {
        const string asm = """
            .org $0100
                HLT
            """;

        await using var session = new MinimalBlinkMachineSession();
        session.SetSelectedSolution(RtcI2cSolution);

        var compile = session.CompileFromSource("rtc-test", asm);
        compile.Success.Should().BeTrue();

        session.LoadAndResetCompiled();

        var snapshot = session.Current;

        snapshot.Rtc.Should().NotBeNull();
        snapshot.Rtc!.I2cAddress.Should().Be(0x68);
        snapshot.Rtc.TimeMode.Should().Be(RtcTimeMode.Simulated);
        snapshot.Rtc.Registers.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task WorkspaceViewModel_ExposesRtcDiagnostics_AfterI2cSolution()
    {
        const string asm = """
            .org $0100
                HLT
            """;

        await using var session = new MinimalBlinkMachineSession();
        var workspace = session.Workspace;
        var vm = workspace.ViewModel.Should().BeOfType<MinimalBlinkWorkspaceViewModel>().Subject;

        session.SetSelectedSolution(RtcI2cSolution);

        var compile = session.CompileFromSource("rtc-test", asm);
        compile.Success.Should().BeTrue();

        session.LoadAndResetCompiled();

        vm.ShowRtcModule.Should().BeTrue();
        vm.RtcI2cAddress.Should().Be("0x68");
        vm.RtcTimeMode.Should().Be("Simulated");
        vm.RtcCurrentTime.Should().NotBeEmpty();
        vm.RtcBusAddress.Should().Be("-");
        vm.RtcRegisters.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task WorkspaceViewModel_ExposesRtcDiagnostics_AfterBusSolution()
    {
        const string asm = """
            .org $0100
                HLT
            """;

        await using var session = new MinimalBlinkMachineSession();
        var workspace = session.Workspace;
        var vm = workspace.ViewModel.Should().BeOfType<MinimalBlinkWorkspaceViewModel>().Subject;

        session.SetSelectedSolution(RtcBusSolution);
        var compile = session.CompileFromSource("rtc-bus-test", asm);
        compile.Success.Should().BeTrue();

        session.LoadAndResetCompiled();

        vm.ShowRtcModule.Should().BeTrue();
        vm.RtcBusAddress.Should().Contain("0xD100");
        vm.RtcI2cAddress.Should().Be("-");
    }

    [TestMethod]
    public async Task SessionSnapshot_RtcSnapshot_ExposesCurrentTime()
    {
        const string asm = """
            .org $0100
                HLT
            """;

        await using var session = new MinimalBlinkMachineSession();
        session.SetSelectedSolution(RtcI2cSolution);

        var compile = session.CompileFromSource("rtc-test", asm);
        compile.Success.Should().BeTrue();

        session.LoadAndResetCompiled();

        var snapshot = session.Current;

        snapshot.Rtc.Should().NotBeNull();
        snapshot.Rtc!.CurrentTime.Should().BeOnOrBefore(DateTime.Now);
    }

    [TestMethod]
    public async Task WorkspaceViewModel_ShowRtcModuleFalse_WhenNoRtcDevice()
    {
        const string asm = """
            .org $0100
                HLT
            """;

        var solution = new SolutionDefinition
        {
            Id = "no-rtc",
            Devices =
            [
                new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
            ]
        };

        await using var session = new MinimalBlinkMachineSession();
        var workspace = session.Workspace;
        var vm = workspace.ViewModel.Should().BeOfType<MinimalBlinkWorkspaceViewModel>().Subject;

        session.SetSelectedSolution(solution);
        var compile = session.CompileFromSource("no-rtc", asm);
        compile.Success.Should().BeTrue();

        session.LoadAndResetCompiled();

        vm.ShowRtcModule.Should().BeFalse();
        vm.RtcRegisters.Should().BeEmpty();
    }

    [TestMethod]
    public void RtcRegisters_ContainAllExpectedOffsets()
    {
        var runtime = new MinimalBlinkHardwareSolutionBuilder().Build(RtcI2cSolution);

        var snapshot = runtime.RtcSnapshot!;

        snapshot.Registers.Length.Should().BeGreaterThanOrEqualTo(16);
        // Time registers (0-6) should be readable BCD values
        for (int i = 0; i <= 6; i++)
        {
            snapshot.Registers[i].Should().BeInRange(0x00, 0x99);
        }
    }
}
