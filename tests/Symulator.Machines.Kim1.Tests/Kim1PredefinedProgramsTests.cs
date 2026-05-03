using FluentAssertions;
using Symulator.Machines.Kim1.Module;
using Symulator.Machines.Kim1.Models;
using Symulator.Machines.Kim1.Services;

namespace Symulator.Machines.Kim1.Tests;

[TestClass]
public sealed class Kim1PredefinedProgramsTests
{
    [TestMethod]
    public void PredefinedPrograms_ShouldContainRamWriteLoop()
    {
        var program = Kim1PredefinedPrograms.FindById("ram-write-loop");

        program.Should().NotBeNull();
        program!.Name.Should().Be("RAM write loop");
        program.LoadAddress.Should().Be(0x0200);
        program.StartAddress.Should().Be(0x0200);
        program.Bytes.Should().Equal(0xA9, 0x42, 0x85, 0x00, 0x4C, 0x04, 0x02);
    }

    [TestMethod]
    public void PredefinedPrograms_FindById_NullEmpty_ReturnsNull()
    {
        Kim1PredefinedPrograms.FindById(null!).Should().BeNull();
        Kim1PredefinedPrograms.FindById("").Should().BeNull();
        Kim1PredefinedPrograms.FindById("   ").Should().BeNull();
    }

    [TestMethod]
    public void PredefinedPrograms_FindById_CaseInsensitive()
    {
        Kim1PredefinedPrograms.FindById("RAM-WRITE-LOOP").Should().NotBeNull();
        Kim1PredefinedPrograms.FindById("Ram_Write_Loop").Should().BeNull();
    }

    [TestMethod]
    public void PredefinedPrograms_All_ShouldContainPrograms()
    {
        Kim1PredefinedPrograms.All.Should().NotBeEmpty();
        Kim1PredefinedPrograms.All.Should().AllBeOfType<Kim1PredefinedProgram>();
    }
}

[TestClass]
public sealed class Kim1MachineSessionPredefinedProgramTests
{
    [TestMethod]
    public async Task LoadPredefinedProgram_UnknownId_ShouldReturnFailure()
    {
        var session = new Kim1MachineSession();

        var result = await session.ExecuteMachineCommandAsync(
            "kim1.load-predefined-program",
            "missing-program");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("missing-program");
    }

    [TestMethod]
    public async Task LoadPredefinedProgram_ShouldSetProgramCounterToStartAddress()
    {
        var session = new Kim1MachineSession();

        var result = await session.ExecuteMachineCommandAsync(
            "kim1.load-predefined-program",
            "ram-write-loop");

        result.IsSuccess.Should().BeTrue();

        var cpu = session.Current.Cpu;
        cpu.Should().NotBeNull();
        cpu!.Pc.Should().Be("$0200");
    }

    [TestMethod]
    public async Task LoadPredefinedProgram_NullParameter_ShouldReturnFailure()
    {
        var session = new Kim1MachineSession();

        var result = await session.ExecuteMachineCommandAsync(
            "kim1.load-predefined-program");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("id is required");
    }

    [TestMethod]
    public async Task LoadedRamWriteLoop_ShouldWriteExpectedValueAfterSteps()
    {
        var session = new Kim1MachineSession();

        var load = await session.ExecuteMachineCommandAsync(
            "kim1.load-predefined-program",
            "ram-write-loop");

        load.IsSuccess.Should().BeTrue();

        var pc = session.Current.Cpu!.Pc;
        pc.Should().Be("$0200");

        var memCheck = session.ReadMemoryForDiagnostics(0x0200);
        memCheck.Should().Be(0xA9, "LDA immediate opcode at start address");

        for (var step = 0; step < 6; step++)
            await session.StepInstructionAsync();

        var ramValue = session.ReadMemoryForDiagnostics(0x0000);
        ramValue.Should().Be(0x42, "program should write $42 to RAM[$0000]");
    }

    [TestMethod]
    public async Task LoadedRamWriteLoop_ShouldEnterLoop()
    {
        var session = new Kim1MachineSession();

        var load = await session.ExecuteMachineCommandAsync(
            "kim1.load-predefined-program",
            "ram-write-loop");

        load.IsSuccess.Should().BeTrue();

        for (var step = 0; step < 8; step++)
            await session.StepInstructionAsync();

        var cpu = session.Current.Cpu;
        cpu.Should().NotBeNull();
        cpu!.Pc.Should().Be("$0204");
    }

    [TestMethod]
    public void PredefinedPrograms_ShouldContainRiotIoTest()
    {
        var program = Kim1PredefinedPrograms.FindById("riot-io-test");

        program.Should().NotBeNull();
        program!.Name.Should().Be("RIOT I/O ports test");
        program.LoadAddress.Should().Be(0x0200);
        program.StartAddress.Should().Be(0x0200);
    }

    [TestMethod]
    public async Task LoadedRiotIoTest_ShouldSetPortValues()
    {
        var session = new Kim1MachineSession();

        var load = await session.ExecuteMachineCommandAsync(
            "kim1.load-predefined-program",
            "riot-io-test");

        load.IsSuccess.Should().BeTrue();

        for (var step = 0; step < 12; step++)
            await session.StepInstructionAsync();

        var portA = session.ReadMemoryForDiagnostics(0x1700);
        var portB = session.ReadMemoryForDiagnostics(0x1702);

        portA.Should().Be(0xAA, "Port A should be set to $AA");
        portB.Should().Be(0x55, "Port B should be set to $55");
    }
}
