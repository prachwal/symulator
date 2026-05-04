using CmosCpu.Cpu.Tests.Reference;

namespace CmosCpu.Cpu.Tests;

[TestClass]
public sealed class Mos6502DecimalReferenceTests
{
    private const string RomName = "decimal_test.bin";
    private const ushort LoadAddress = 0x0000;
    private const ushort ResetVector = 0x0200;
    private const int MaxSteps = 100_000_000;

    [TestMethod]
    public void KlausDormann_DecimalTest_Passes_WhenRomIsPresent()
    {
        var romPath = Reference6502TestRunner.DefaultRomPath(RomName);

        if (!File.Exists(romPath))
        {
            Assert.Inconclusive($"Reference ROM not found: {romPath}. " +
                "Download from https://github.com/Klaus2m5/6502_65C02_functional_tests");
            return;
        }

        ReferenceTestResult result = Reference6502TestRunner.Run(
            romPath,
            LoadAddress,
            ResetVector,
            MaxSteps,
            (ram, cpu) =>
            {
                byte error = ram.ReadByte(0x0B);
                if (error == 0x00)
                    return ReferenceTestState.Success;
                byte nextOpcode = ram.ReadByte(cpu.PC);
                if (nextOpcode == 0xDB)
                    return ReferenceTestState.Failed;
                return ReferenceTestState.Continue;
            });

        if (result.RomMissing)
        {
            Assert.Inconclusive($"Reference ROM not found: {romPath}");
            return;
        }

        Assert.IsTrue(result.Success,
            $"Decimal test failed: {result.Error}. " +
            $"Steps={result.StepsExecuted}, PC=0x{result.PC:X4}, " +
            $"A=0x{result.A:X2}, X=0x{result.X:X2}, Y=0x{result.Y:X2}, SP=0x{result.SP:X2}");
    }
}
