using CmosCpu.Terminal.Commands;
using CmosCpu.Terminal.Session;
using FluentAssertions;
using Moq;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class BatchCommandsTests
{
    private Mock<ISimulatorSession> CreateMockSession(bool loaded = true, bool running = false, bool canAutoLoad = true)
    {
        var mock = new Mock<ISimulatorSession>(MockBehavior.Strict);
        mock.SetupGet(s => s.IsLoaded).Returns(loaded);
        mock.SetupGet(s => s.IsRunning).Returns(running);
        if (!loaded)
            mock.Setup(s => s.LoadProfile(It.IsAny<string>())).Returns(canAutoLoad);
        return mock;
    }

    [TestMethod]
    public void HandleHelp_ReturnsZero()
    {
        BatchCommands.HandleHelp([]).Should().Be(0);
    }

    [TestMethod]
    public void HandleVersion_ReturnsZero()
    {
        BatchCommands.HandleVersion([]).Should().Be(0);
    }

    [TestMethod]
    public void HandleState_NoMachine_AutoLoadFails_ReturnsError()
    {
        var mock = CreateMockSession(loaded: false, canAutoLoad: false);

        int result = BatchCommands.HandleState(mock.Object, []);

        result.Should().Be(1);
    }

    [TestMethod]
    public void HandleReset_NoMachine_AutoLoadFails_ReturnsError()
    {
        var mock = CreateMockSession(loaded: false, canAutoLoad: false);

        int result = BatchCommands.HandleReset(mock.Object, []);
        result.Should().Be(1);
    }

    [TestMethod]
    public void HandleReset_WithMachine_CallsReset()
    {
        var mock = CreateMockSession();
        mock.Setup(s => s.Reset());

        int result = BatchCommands.HandleReset(mock.Object, []);
        result.Should().Be(0);
        mock.Verify(s => s.Reset(), Times.Once);
    }

    [TestMethod]
    public void HandleStep_NoMachine_AutoLoadFails_ReturnsError()
    {
        var mock = CreateMockSession(loaded: false, canAutoLoad: false);

        int result = BatchCommands.HandleStep(mock.Object, []);
        result.Should().Be(1);
    }

    [TestMethod]
    public void HandleStep_Running_ReturnsError()
    {
        var mock = CreateMockSession(loaded: true, running: true);
        mock.Setup(s => s.LoadProfile(It.IsAny<string>())).Returns(true);

        int result = BatchCommands.HandleStep(mock.Object, []);
        result.Should().Be(3);
    }

    [TestMethod]
    public void HandlePause_StopsSession()
    {
        var mock = CreateMockSession(running: true);
        mock.Setup(s => s.Stop());

        int result = BatchCommands.HandlePause(mock.Object, []);
        result.Should().Be(0);
        mock.Verify(s => s.Stop(), Times.Once);
    }

    [TestMethod]
    public void HandlePause_NotRunning_ReturnsSuccess()
    {
        var mock = CreateMockSession();

        int result = BatchCommands.HandlePause(mock.Object, []);
        result.Should().Be(0);
    }

    [TestMethod]
    public void HandleSpeed_WithoutArgs_ShowsCurrent()
    {
        var mock = CreateMockSession();
        mock.Setup(s => s.GetSpeed()).Returns((1000, 16));

        int result = BatchCommands.HandleSpeed(mock.Object, []);
        result.Should().Be(0);
    }

    [TestMethod]
    public void HandleSpeed_WithBatch_SetsSpeed()
    {
        var mock = CreateMockSession();
        mock.Setup(s => s.SetSpeed(500, 16));
        mock.Setup(s => s.GetSpeed()).Returns((500, 16));

        int result = BatchCommands.HandleSpeed(mock.Object, ["--batch", "500"]);
        result.Should().Be(0);
        mock.Verify(s => s.SetSpeed(500, 16), Times.AtLeastOnce);
    }

    [TestMethod]
    public void HandleMemory_NoMachine_AutoLoadFails_ReturnsError()
    {
        var mock = CreateMockSession(loaded: false, canAutoLoad: false);

        int result = BatchCommands.HandleMemory(mock.Object, []);
        result.Should().Be(1);
    }

    [TestMethod]
    public void HandlerRegisters_NoMachine_AutoLoadFails_ReturnsError()
    {
        var mock = CreateMockSession(loaded: false, canAutoLoad: false);

        int result = BatchCommands.HandleRegisters(mock.Object, []);
        result.Should().Be(1);
    }

    [TestMethod]
    public void HandleTerminal_NoMachine_AutoLoadFails_ReturnsError()
    {
        var mock = CreateMockSession(loaded: false, canAutoLoad: false);

        int result = BatchCommands.HandleTerminal(mock.Object, []);

        result.Should().Be(1);
    }

    [TestMethod]
    public void HandleApple1Basic_NoProfile_ReturnsValidationError()
    {
        var mock = CreateMockSession(loaded: false, canAutoLoad: true);

        int result = BatchCommands.HandleApple1Basic(mock.Object, []);

        result.Should().Be(1);
    }
}
