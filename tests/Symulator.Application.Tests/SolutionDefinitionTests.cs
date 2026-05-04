using Symulator.Application.Solutions;
using FluentAssertions;

namespace Symulator.Application.Tests;

[TestClass]
public sealed class SolutionDefinitionTests
{
    [TestMethod]
    public void VisibleDeviceTypes_ReturnsOnlyVisibleDevices()
    {
        var solution = new SolutionDefinition
        {
            Devices =
            [
                new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
                new DeviceDefinition { Id = "led0", Type = "led-mmio", Visible = true },
                new DeviceDefinition { Id = "hidden", Type = "debug", Visible = false },
            ]
        };

        solution.VisibleDeviceTypes.Should().BeEquivalentTo("cpu", "led-mmio");
    }

    [TestMethod]
    public void AllDeviceTypes_ReturnsAllDevices()
    {
        var solution = new SolutionDefinition
        {
            Devices =
            [
                new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
                new DeviceDefinition { Id = "debug", Type = "debug", Visible = false },
            ]
        };

        solution.AllDeviceTypes.Should().BeEquivalentTo("cpu", "debug");
    }

    [TestMethod]
    public void HasVisibleDevice_ReturnsTrueOnlyForVisibleTypes()
    {
        var solution = new SolutionDefinition
        {
            Devices =
            [
                new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
                new DeviceDefinition { Id = "hidden", Type = "debug", Visible = false },
            ]
        };

        solution.HasVisibleDevice("cpu").Should().BeTrue();
        solution.HasVisibleDevice("debug").Should().BeFalse();
    }

    [TestMethod]
    public void HasRuntimeDevice_ReturnsTrueForAllDeclaredTypes()
    {
        var solution = new SolutionDefinition
        {
            Devices =
            [
                new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
                new DeviceDefinition { Id = "hidden", Type = "debug", Visible = false },
            ]
        };

        solution.HasRuntimeDevice("cpu").Should().BeTrue();
        solution.HasRuntimeDevice("debug").Should().BeTrue();
        solution.HasRuntimeDevice("non-existent").Should().BeFalse();
    }

    [TestMethod]
    public void HasDevice_BackwardCompatible_ReturnsTrueForAllDeclaredTypes()
    {
        var solution = new SolutionDefinition
        {
            Devices =
            [
                new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
            ]
        };

        solution.HasDevice("cpu").Should().BeTrue();
    }

    [TestMethod]
    public void VisibleDeviceTypes_IsEmpty_WhenNoVisibleDevices()
    {
        var solution = new SolutionDefinition
        {
            Devices =
            [
                new DeviceDefinition { Id = "hidden", Type = "debug", Visible = false },
            ]
        };

        solution.VisibleDeviceTypes.Should().BeEmpty();
    }

    [TestMethod]
    public void AllDeviceTypes_IsEmpty_WhenNoDevices()
    {
        var solution = new SolutionDefinition
        {
            Devices = []
        };

        solution.AllDeviceTypes.Should().BeEmpty();
        solution.VisibleDeviceTypes.Should().BeEmpty();
    }

    [TestMethod]
    public void Fallback_ContainsOnlyCpuDevice()
    {
        var fallback = SolutionDefinition.CreateFallback("test", "test.asm");

        fallback.VisibleDeviceTypes.Should().BeEquivalentTo("cpu");
        fallback.AllDeviceTypes.Should().BeEquivalentTo("cpu");
    }
}
