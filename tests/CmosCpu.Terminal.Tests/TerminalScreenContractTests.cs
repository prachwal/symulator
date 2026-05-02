using CmosCpu.Terminal.Tui;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class TerminalScreenContractTests
{
    [TestMethod]
    public void ITerminalScreen_InterfaceExists()
    {
        typeof(ITerminalScreen).IsInterface.Should().BeTrue();
        typeof(ITerminalScreen).GetMethods().Select(m => m.Name).Should().Contain("Run");
    }

    [TestMethod]
    public void Kim1TuiScreen_ImplementsITerminalScreen()
    {
        typeof(Kim1TuiScreen).IsAssignableTo(typeof(ITerminalScreen)).Should().BeTrue();
    }

    [TestMethod]
    public void Apple1TuiScreen_ImplementsITerminalScreen()
    {
        typeof(Apple1TuiScreen).IsAssignableTo(typeof(ITerminalScreen)).Should().BeTrue();
    }

    [TestMethod]
    public void Kim1KeyMapper_IsStaticClass()
    {
        typeof(Kim1KeyMapper).IsAbstract.Should().BeTrue();
        typeof(Kim1KeyMapper).IsSealed.Should().BeTrue();
    }
}
