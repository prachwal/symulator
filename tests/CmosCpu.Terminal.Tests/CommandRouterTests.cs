using CmosCpu.Terminal.Commands;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class CommandRouterTests
{
    [TestMethod]
    public void Register_And_Resolve_Command()
    {
        var router = new CommandRouter();
        bool called = false;

        router.Register("test", args => { called = true; return 42; });
        var handler = router.Resolve("test");

        handler.Should().NotBeNull();
        handler!(["arg"]).Should().Be(42);
        called.Should().BeTrue();
    }

    [TestMethod]
    public void Resolve_Unknown_ReturnsNull()
    {
        var router = new CommandRouter();
        router.Resolve("nonexistent").Should().BeNull();
    }

    [TestMethod]
    public void RegisterAlias_ResolvesToSameHandler()
    {
        var router = new CommandRouter();
        router.Register("original", args => 99);
        router.RegisterAlias("alias", "original");

        router.Resolve("alias").Should().NotBeNull();
        router.Resolve("alias")!(["x"]).Should().Be(99);
    }

    [TestMethod]
    public void Execute_UnknownCommand_Returns1()
    {
        var router = new CommandRouter();
        int result = router.Execute(["unknown"]);
        result.Should().Be(1);
    }

    [TestMethod]
    public void Execute_NoArgs_Returns0()
    {
        var router = new CommandRouter();
        router.Register("help", args => 0);
        int result = router.Execute([]);
        result.Should().Be(0);
    }

    [TestMethod]
    public void Execute_KnownCommand_ReturnsHandlerResult()
    {
        var router = new CommandRouter();
        router.Register("test", args => 5);

        router.Execute(["test"]).Should().Be(5);
    }
}
