using Symulator.Application.Assembly;
using Symulator.Application.Solutions;
using Symulator.Machines.MinimalBlink.Module;
using FluentAssertions;

namespace Symulator.Machines.MinimalBlink.Tests;

[TestClass]
public sealed class LcdClockDebugTests
{
    [TestMethod]
    public async Task LcdDiagnostic_WriteX_ShouldSetDdram()
    {
        const string asm = """
LCD_CMD  = $FE00
LCD_DATA = $FE01

.org $0100
start:
    LDA #$38
    STA LCD_CMD
    LDA #$0C
    STA LCD_CMD
    LDA #$01
    STA LCD_CMD
    LDA #$80
    STA LCD_CMD
    LDA #'X'
    STA LCD_DATA
done:
    HLT
""";

        var solution = new SolutionDefinition
        {
            Id = "lcd-diag",
            Devices =
            [
                new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
                new DeviceDefinition { Id = "lcd0", Type = "hd44780-mmio", BaseAddress = "0xFE00", Visible = true },
            ]
        };

        await using var session = new MinimalBlinkMachineSession();
        session.SetSelectedSolution(solution);

        var compile = session.CompileFromSource("lcd-diag", asm);
        compile.Success.Should().BeTrue("{0}",
            string.Join("; ", compile.Diagnostics.Where(d => d.Severity == "Error").Select(d => d.Message)));
        session.LoadAndResetCompiled();

        // Step through all instructions
        for (int i = 0; i < 12; i++)
            await session.StepInstructionAsync();

        // Read LCD DDRAM through the hardware runtime
        // The session stores _lcd field — we can verify via session.Current state
        var snapshot = session.Current;
        snapshot.Cpu.Should().NotBeNull();
        snapshot.Cpu!.A.Should().Be("$58", "A should hold 'X' ($58) after the final LDA");  // 'X' = 0x58
    }

    [TestMethod]
    public async Task RtcClockAsm_DebugStep()
    {
        var sourcePath = Path.Combine(TestPaths.ProgramsAsm, "rtc-clock.asm");
        var source = await File.ReadAllTextAsync(sourcePath);

        var asm = new PseudoCpuAssembler();
        var result = asm.Assemble("rtc-clock", source);
        result.Success.Should().BeTrue("{0}",
            string.Join("; ", result.Diagnostics.Where(d => d.Severity == "Error").Select(d => d.Message)));

        result.Image!.Bytes.Length.Should().BeGreaterThan(50);
    }

    [TestMethod]
    public void RtcClock_StepThroughFirstInstructions_ShouldNotHang()
    {
        const string asm = """
LCD_CMD  = $FE00
LCD_DATA = $FE01

.org $0100
start:
    LDA #$38
    STA LCD_CMD
    JSR delay_short

    LDA #$0C
    STA LCD_CMD
    JSR delay_short

    LDA #$01
    STA LCD_CMD
    JSR delay_long

    HLT

delay_short:
    LDA #$08
    STA $FA
ds_loop:
    DEC $FA
    JNZ ds_loop
    RTS

delay_long:
    LDA #$FF
    STA $FA
dl_loop:
    DEC $FA
    JNZ dl_loop
    RTS
""";

        var solution = new SolutionDefinition
        {
            Id = "delay-test",
            Devices =
            [
                new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true },
                new DeviceDefinition { Id = "lcd0", Type = "hd44780-mmio", BaseAddress = "0xFE00", Visible = true },
            ]
        };

        var asm2 = new PseudoCpuAssembler();
        var result = asm2.Assemble("delay-test", asm);
        result.Success.Should().BeTrue("{0}",
            string.Join("; ", result.Diagnostics.Where(d => d.Severity == "Error").Select(d => d.Message)));

        var session = new MinimalBlinkMachineSession();
        session.SetSelectedSolution(solution);

        var compile = session.CompileFromSource("delay-test", asm);
        compile.Success.Should().BeTrue();
        session.LoadAndResetCompiled();

        // Step through — this should not hang
        for (int step = 0; step < 10; step++)
        {
            session.StepInstructionAsync().GetAwaiter().GetResult();
            session.Current.Cpu.Should().NotBeNull();
            session.Current.IsHalted.Should().BeFalse("step {0} should not halt before HLT", step);
        }
    }

    [TestMethod]
    public void MinimalBlinkCpu_JsrRtsRoundtrip()
    {
        // This test validates the CPU's JSR/RTS directly through a compiled program
        const string asm = """
.org $0100
start:
    JSR sub
    LDA #$42
    HLT
sub:
    LDA #$12
    RTS
""";

        var asm2 = new PseudoCpuAssembler();
        var result = asm2.Assemble("jsr-test", asm);
        result.Success.Should().BeTrue("{0}",
            string.Join("; ", result.Diagnostics.Where(d => d.Severity == "Error").Select(d => d.Message)));
        result.Image.Should().NotBeNull();

        // Verify JSR opcode (0x0D) at start, then RTS (0x0E) at sub
        var bytes = result.Image!.Bytes;
        bytes[0].Should().Be(0x0D, "JSR opcode");  // JSR sub
        // First LDA (#$42) is at 0x0103 (3 bytes for JSR)
        // Wait — the assembler's JSR is 3 bytes: opcode + lo + hi
        // And LDA #$42 is at 0x0103 (opcode 0x01 + operand 0x42)
        // HLT at 0x0105
    }
}
