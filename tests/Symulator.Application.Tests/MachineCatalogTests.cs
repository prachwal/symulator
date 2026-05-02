using FluentAssertions;
using Moq;
using Symulator.Application.Abstractions;
using Symulator.Application.Services;

namespace Symulator.Application.Tests;

[TestClass]
public sealed class MachineCatalogTests
{
    [TestMethod]
    public void GetMachines_ReturnsMachinesFromAllModules()
    {
        var module1 = new Mock<IMachineModule>();
        module1.Setup(m => m.GetMachines()).Returns(new List<MachineDescriptor>
        {
            new("m1", "Machine 1", "Fam1", "p1.json")
        });

        var module2 = new Mock<IMachineModule>();
        module2.Setup(m => m.GetMachines()).Returns(new List<MachineDescriptor>
        {
            new("m2", "Machine 2", "Fam2", "p2.json")
        });

        var catalog = new MachineCatalog(new[] { module1.Object, module2.Object });

        var machines = catalog.GetMachines();

        machines.Should().HaveCount(2);
        machines.Should().Contain(m => m.Id == "m1");
        machines.Should().Contain(m => m.Id == "m2");
    }

    [TestMethod]
    public void FindModule_ReturnsCorrectModule()
    {
        var module = new Mock<IMachineModule>();
        module.Setup(m => m.GetMachines()).Returns(new List<MachineDescriptor>
        {
            new("apple1", "Apple-1", "Apple", "p.json")
        });
        module.Setup(m => m.Id).Returns("apple-module");

        var catalog = new MachineCatalog(new[] { module.Object });

        var found = catalog.FindModule("apple1");
        found.Should().NotBeNull();
        found!.Id.Should().Be("apple-module");
    }

    [TestMethod]
    public void FindModule_ReturnsNull_WhenNotFound()
    {
        var catalog = new MachineCatalog(Enumerable.Empty<IMachineModule>());

        var found = catalog.FindModule("nonexistent");
        found.Should().BeNull();
    }
}

[TestClass]
public sealed class EmulatorControllerTests
{
    private static Mock<IMachineSession> CreateSessionMock(string id, string name)
    {
        var session = new Mock<IMachineSession>();
        session.Setup(s => s.MachineId).Returns(id);
        session.Setup(s => s.DisplayName).Returns(name);
        session.Setup(s => s.Current).Returns(new EmulatorStateSnapshot(id, false, false, null, "", 0));
        session.Setup(s => s.ResetAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        session.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return session;
    }

    [TestMethod]
    public async Task SelectMachineAsync_CreatesSession()
    {
        var session = CreateSessionMock("m1", "Machine 1");

        var module = new Mock<IMachineModule>();
        module.Setup(m => m.GetMachines()).Returns(new List<MachineDescriptor> { new("m1", "Machine 1", "F", "p.json") });
        module.Setup(m => m.CreateSessionAsync("m1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);

        var controller = new EmulatorController(new MachineCatalog(new[] { module.Object }));

        var result = await controller.SelectMachineAsync("m1");

        result.Should().BeTrue();
        controller.ActiveSession.Should().NotBeNull();
        controller.ActiveSession!.MachineId.Should().Be("m1");
    }

    [TestMethod]
    public async Task SelectMachineAsync_ReturnsFalse_WhenModuleNotFound()
    {
        var catalog = new MachineCatalog(Enumerable.Empty<IMachineModule>());
        var controller = new EmulatorController(catalog);

        var result = await controller.SelectMachineAsync("nonexistent");

        result.Should().BeFalse();
        controller.ActiveSession.Should().BeNull();
    }

    [TestMethod]
    public async Task RunAsync_DelegatesToActiveSession()
    {
        var session = CreateSessionMock("m1", "M1");
        session.Setup(s => s.RunAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var module = new Mock<IMachineModule>();
        module.Setup(m => m.GetMachines()).Returns(new List<MachineDescriptor> { new("m1", "M1", "F", "p.json") });
        module.Setup(m => m.CreateSessionAsync("m1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);

        var controller = new EmulatorController(new MachineCatalog(new[] { module.Object }));
        await controller.SelectMachineAsync("m1");

        await controller.RunAsync();

        session.Verify(s => s.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task ResetAsync_DelegatesToActiveSession()
    {
        var session = CreateSessionMock("m1", "M1");

        var module = new Mock<IMachineModule>();
        module.Setup(m => m.GetMachines()).Returns(new List<MachineDescriptor> { new("m1", "M1", "F", "p.json") });
        module.Setup(m => m.CreateSessionAsync("m1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);

        var controller = new EmulatorController(new MachineCatalog(new[] { module.Object }));
        await controller.SelectMachineAsync("m1");

        await controller.ResetAsync();

        session.Verify(s => s.ResetAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task SelectMachineAsync_DisposesPreviousSession()
    {
        var session1 = CreateSessionMock("m1", "M1");
        var session2 = CreateSessionMock("m2", "M2");

        var module = new Mock<IMachineModule>();
        module.Setup(m => m.GetMachines()).Returns(new List<MachineDescriptor>
        {
            new("m1", "M1", "F", "p.json"),
            new("m2", "M2", "F", "p.json")
        });
        module.Setup(m => m.CreateSessionAsync("m1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session1.Object);
        module.Setup(m => m.CreateSessionAsync("m2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session2.Object);

        var controller = new EmulatorController(new MachineCatalog(new[] { module.Object }));

        await controller.SelectMachineAsync("m1");
        await controller.SelectMachineAsync("m2");

        session1.Verify(s => s.DisposeAsync(), Times.Once);
        controller.ActiveSession.Should().Be(session2.Object);
    }

    [TestMethod]
    public async Task StateChanged_FiresOnSessionStateChange()
    {
        var snapshot = new EmulatorStateSnapshot("m1", false, false, null, "", 0);
        var session = CreateSessionMock("m1", "M1");

        var module = new Mock<IMachineModule>();
        module.Setup(m => m.GetMachines()).Returns(new List<MachineDescriptor> { new("m1", "M1", "F", "p.json") });
        module.Setup(m => m.CreateSessionAsync("m1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);

        var controller = new EmulatorController(new MachineCatalog(new[] { module.Object }));
        await controller.SelectMachineAsync("m1");

        EmulatorStateSnapshot? received = null;
        controller.StateChanged += (_, s) => received = s;

        session.Raise(s => s.StateChanged += null, session.Object, snapshot);

        received.Should().NotBeNull();
        received!.MachineId.Should().Be("m1");
    }
}
