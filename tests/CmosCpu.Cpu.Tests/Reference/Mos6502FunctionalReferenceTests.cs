using CmosCpu.Cpu.Tests.Reference;

namespace CmosCpu.Cpu.Tests;

[TestClass]
public sealed class Mos6502FunctionalReferenceTests
{
    private const string RomName = "6502_functional_test.bin";
    private const ushort LoadAddress = 0x0000;
    private const ushort ResetVector = 0x0400;
    private const ushort SuccessTrapPc = 0x3469;
    private const int MaxSteps = 100_000_000;

    [TestMethod]
    public void KlausDormann_6502FunctionalTest_Passes_WhenRomIsPresent()
    {
        var romPath = Reference6502TestRunner.DefaultRomPath(RomName);

        if (!File.Exists(romPath))
        {
            Assert.Inconclusive($"Reference ROM not found: {romPath}. " +
                "Download from https://github.com/Klaus2m5/6502_65C02_functional_tests");
            return;
        }

        // Klaus Dormann's no-I/O functional test reports completion by entering
        // a self-loop. For the bundled bin_files/6502_functional_test.bin image,
        // the successful completion trap is PC=$3469. Error traps also self-loop,
        // but at the failing test location; do not use a memory sentinel here.
        ReferenceTestResult result = Reference6502TestRunner.Run(
            romPath,
            LoadAddress,
            ResetVector,
            MaxSteps,
            (_, cpu) => cpu.PC == SuccessTrapPc
                ? ReferenceTestState.Success
                : ReferenceTestState.Continue);

        if (result.RomMissing)
        {
            Assert.Inconclusive($"Reference ROM not found: {romPath}");
            return;
        }

        Assert.IsTrue(result.Success,
            $"6502 functional test failed: {result.Error}. " +
            $"Expected success trap PC=0x{SuccessTrapPc:X4}. " +
            $"Steps={result.StepsExecuted}, PC=0x{result.PC:X4}, " +
            $"A=0x{result.A:X2}, X=0x{result.X:X2}, Y=0x{result.Y:X2}, SP=0x{result.SP:X2}");
    }
}
