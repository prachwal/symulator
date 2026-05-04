using FluentAssertions;
using Symulator.Machines.MinimalBlink.Module;

namespace Symulator.Machines.MinimalBlink.Tests;

[TestClass]
public sealed class HaltedCpuRunLoopTests
{
    [TestMethod]
    public async Task HaltedCpu_StopsRunLoop()
    {
        await using var session = new MinimalBlinkMachineSession();

        var loadResult = await session.ExecuteMachineCommandAsync(
            "minimal-blink.load-predefined-program",
            "led-on");
        loadResult.IsSuccess.Should().BeTrue();

        for (var i = 0; i < 10 && !session.Current.IsHalted; i++)
            await session.StepInstructionAsync();

        session.Current.IsHalted.Should().BeTrue();

        await session.RunAsync();

        session.Current.IsRunning.Should().BeFalse();
        session.Current.IsHalted.Should().BeTrue();
    }
}
