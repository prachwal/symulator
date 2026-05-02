using System.Reflection;
using CmosCpu.Core;
using CmosCpu.Cpu;
using CmosCpu.Terminal.Tui;
using FluentAssertions;
using Moq;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class TerminalGuiRendererTests
{
    [TestMethod]
    public void FormatStatus_ReturnsCorrectStrings()
    {
        TerminalGuiRenderer.FormatStatus(true, false).Should().Be("RUNNING");
        TerminalGuiRenderer.FormatStatus(false, true).Should().Be("HALTED");
        TerminalGuiRenderer.FormatStatus(false, false).Should().Be("STOPPED");
    }

    [TestMethod]
    public void FormatRegistersLine_ContainsRegisterValues()
    {
        var cpu = CreateCpuWithRegisterState(0xAB, 0xCD, 0xEF, 0x1234);
        string line = TerminalGuiRenderer.FormatRegistersLine(cpu);
        line.Should().Contain("PC=0x1234");
        line.Should().Contain("A=0xAB");
        line.Should().Contain("X=0xCD");
        line.Should().Contain("Y=0xEF");
    }

    [TestMethod]
    public void FormatFlags_ContainsAllFlags()
    {
        var cpu = CreateCpuWithFlagState(true, false, true, false, true, false, true);
        string flags = TerminalGuiRenderer.FormatFlags(cpu);
        flags.Should().Contain("NV-BDIZC");
        flags.Should().Match("*1*0*1*0*1*0*1*");
    }

    private static Mos6502Cpu CreateCpuWithRegisterState(byte a, byte x, byte y, ushort pc)
    {
        var busMock = new Mock<IMemoryBus>();
        var cpu = new Mos6502Cpu(busMock.Object);

        SetField(cpu, "A", a);
        SetField(cpu, "X", x);
        SetField(cpu, "Y", y);
        SetField(cpu, "PC", pc);

        return cpu;
    }

    private static Mos6502Cpu CreateCpuWithFlagState(
        bool carry, bool zero, bool interruptDisable, bool decimalMode,
        bool breakMode, bool overflow, bool negative)
    {
        var busMock = new Mock<IMemoryBus>();
        var cpu = new Mos6502Cpu(busMock.Object);

        SetField(cpu, "Carry", carry);
        SetField(cpu, "Zero", zero);
        SetField(cpu, "InterruptDisable", interruptDisable);
        SetField(cpu, "Decimal", decimalMode);
        SetField(cpu, "Break", breakMode);
        SetField(cpu, "Overflow", overflow);
        SetField(cpu, "Negative", negative);

        return cpu;
    }

    private static void SetField(object obj, string name, object value)
    {
        var field = typeof(Mos6502Cpu).GetField(
            $"<{name}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null)
        {
            var prop = typeof(Mos6502Cpu).GetProperty(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var setter = prop?.GetSetMethod(true);
            if (setter is not null)
            {
                setter.Invoke(obj, [value]);
                return;
            }
        }
        field?.SetValue(obj, value);
    }
}
