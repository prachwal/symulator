using Symulator.Application.Solutions;
using Symulator.Machines.MinimalBlink.Hardware;
using FluentAssertions;

namespace Symulator.Machines.MinimalBlink.Tests;

[TestClass]
public sealed class SolutionProfileIntegrationTests
{
    private readonly MinimalBlinkHardwareSolutionBuilder _builder = new();
    private readonly SolutionDefinitionLoader _loader = new();

    private async Task<MinimalBlinkHardwareRuntime> LoadAndBuildAsync(string solutionName)
    {
        var sourcePath = Path.Combine(TestPaths.ProgramsAsm, solutionName + ".asm");
        var solution = await _loader.LoadForSourceAsync(sourcePath);
        return _builder.Build(solution);
    }

    [TestMethod]
    public async Task BlinkProfile_BuildsCpuAndLed()
    {
        var runtime = await LoadAndBuildAsync("blink");

        runtime.Cpu.Should().NotBeNull();
        runtime.HasVisibleDevice("cpu").Should().BeTrue();
        runtime.HasVisibleDevice("led-mmio").Should().BeTrue();
        runtime.HasVisibleDevice("uart-mmio").Should().BeFalse();
        runtime.HasRuntimeInstance("cpu").Should().BeTrue();
        runtime.HasRuntimeInstance("led-mmio").Should().BeTrue();
    }

    [TestMethod]
    public async Task HelloUartProfile_BuildsCpuAndUart()
    {
        var runtime = await LoadAndBuildAsync("hello-uart");

        runtime.Cpu.Should().NotBeNull();
        runtime.HasVisibleDevice("cpu").Should().BeTrue();
        runtime.HasVisibleDevice("uart-mmio").Should().BeTrue();
        runtime.Uart.Should().NotBeNull();
        runtime.Terminal.Should().NotBeNull();
        runtime.HasVisibleDevice("led-mmio").Should().BeFalse();
    }

    [TestMethod]
    public async Task HelloLcdProfile_BuildsCpuAndLcd()
    {
        var runtime = await LoadAndBuildAsync("hello-lcd");

        runtime.Cpu.Should().NotBeNull();
        runtime.HasVisibleDevice("cpu").Should().BeTrue();
        runtime.HasVisibleDevice("hd44780-mmio").Should().BeTrue();
        runtime.Lcd.Should().NotBeNull();
        runtime.LcdBuffer.Should().NotBeNull();
        runtime.HasVisibleDevice("uart-mmio").Should().BeFalse();
    }

    [TestMethod]
    public async Task HelloLcdI2cProfile_BuildsI2cHardware()
    {
        var runtime = await LoadAndBuildAsync("hello-lcd-i2c");

        runtime.Cpu.Should().NotBeNull();
        runtime.HasVisibleDevice("cpu").Should().BeTrue();
        runtime.HasVisibleDevice("i2c-controller-mmio").Should().BeTrue();
        runtime.HasVisibleDevice("hd44780-pcf8574").Should().BeTrue();
        runtime.I2cBus.Should().NotBeNull();
        runtime.Lcd.Should().NotBeNull();
        runtime.LcdBuffer.Should().NotBeNull();
    }

    [TestMethod]
    public async Task I2cScanProfile_BuildsI2cAndUart()
    {
        var runtime = await LoadAndBuildAsync("i2c-scan");

        runtime.Cpu.Should().NotBeNull();
        runtime.HasVisibleDevice("cpu").Should().BeTrue();
        runtime.HasVisibleDevice("i2c-controller-mmio").Should().BeTrue();
        runtime.HasVisibleDevice("uart-mmio").Should().BeTrue();
        runtime.I2cBus.Should().NotBeNull();
        runtime.I2cController.Should().NotBeNull();
        runtime.Uart.Should().NotBeNull();
        runtime.Terminal.Should().NotBeNull();
    }

    [TestMethod]
    public async Task LoadedSolution_HasSourceFilePath()
    {
        var sourcePath = Path.Combine(TestPaths.ProgramsAsm, "blink.asm");
        var solution = await _loader.LoadForSourceAsync(sourcePath);

        solution.SourceFilePath.Should().NotBeNull();
        File.Exists(solution.SourceFilePath).Should().BeTrue();
    }
}
