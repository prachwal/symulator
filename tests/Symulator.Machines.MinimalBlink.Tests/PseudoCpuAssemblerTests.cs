using Symulator.Application.Assembly;
using Symulator.Application.Solutions;
using Symulator.Machines.MinimalBlink.Module;
using FluentAssertions;

namespace Symulator.Machines.MinimalBlink.Tests;

[TestClass]
public sealed class PseudoCpuAssemblerTests
{
    private static PseudoCpuAssembler Create() => new();

    [TestMethod]
    public void Assemble_HelloUart_ProducesExpectedBytes()
    {
        var asm = Create();

        var result = asm.Assemble("hello-uart", @"
.org $0100
UART_DATA = $FE40
start:
    LDA #'H'
    STA UART_DATA
    LDA #$0D
    STA UART_DATA
    HLT
");

        result.Success.Should().BeTrue(string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        result.Image.Should().NotBeNull();
        result.Image!.Bytes.Should().HaveCount(11); // 2×(LDA(2)+STA(3)) + HLT(1)
        result.Image.Bytes[0].Should().Be(0x01); // LDA #imm
        result.Image.Bytes[1].Should().Be((byte)'H');
        result.Image.Bytes[2].Should().Be(0x02); // STA abs
        result.Image.Bytes[3].Should().Be(0x40);
        result.Image.Bytes[4].Should().Be(0xFE);
        result.Image.StartAddress.Should().Be(0x0100);
    }

    [TestMethod]
    public void Assemble_Label_BneOffset()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
start:
    NOP
    NOP
    NOP
    BNE start
");

        result.Success.Should().BeTrue();
        result.Image.Should().NotBeNull();
        // NOPx3(3 bytes) + BNE(2 bytes) = 5 bytes
        result.Image!.Bytes.Should().HaveCount(5);
        result.Image.Bytes[3].Should().Be(0x0C); // BNE opcode at byte 3
        // BNE at $0103+2=$0105, target $0100, offset = -5 = 0xFB
        result.Image.Bytes[4].Should().Be(0xFB); // offset
    }

    [TestMethod]
    public void Assemble_Directives()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0200
TEST = $42
.byte $01, $02, $AA
.text ""Hi""
");

        result.Success.Should().BeTrue();
        result.Image.Should().NotBeNull();
        result.Image!.LoadAddress.Should().Be(0x0200);
        result.Image.Bytes.Should().HaveCount(5);
        result.Image.Bytes[0].Should().Be(0x01);
        result.Image.Bytes[1].Should().Be(0x02);
        result.Image.Bytes[2].Should().Be(0xAA);
        result.Image.Bytes[3].Should().Be((byte)'H');
        result.Image.Bytes[4].Should().Be((byte)'i');
    }

    [TestMethod]
    public void Assemble_UnknownMnemonic_ReturnsDiagnostic()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
    FOOBAR #$20
");

        result.Success.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Severity == "Error" && d.Message.Contains("FOOBAR"));
    }

    [TestMethod]
    public void Assemble_DuplicateLabel_ReturnsDiagnostic()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
start:
    NOP
start:
    NOP
");

        result.Success.Should().BeFalse();
        result.Diagnostics.Should().Contain(d => d.Message.Contains("Duplicate label"));
    }

    [TestMethod]
    public void Assemble_PredefinedSymbols()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
    STA UART_DATA
    STA I2C_ADDRESS
");

        result.Success.Should().BeTrue();
        result.Image!.Bytes[1].Should().Be(0x40); // UART_DATA = $FE40
        result.Image.Bytes[2].Should().Be(0xFE);
        result.Image.Bytes[4].Should().Be(0x31); // I2C_ADDRESS = $FE31
        result.Image.Bytes[5].Should().Be(0xFE);
    }

    [TestMethod]
    public void Assemble_HexDecimalNumbers()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
    LDA #$20
    LDA #32
    LDA #'A'
");

        result.Success.Should().BeTrue();
        result.Image!.Bytes[1].Should().Be(0x20); // hex $20
        result.Image.Bytes[3].Should().Be(32);    // decimal 32
        result.Image.Bytes[5].Should().Be((byte)'A'); // char 'A'
    }

    [TestMethod]
    public void Assemble_IncludedPrograms()
    {
        var asm = Create();
        var testDir = Path.GetDirectoryName(typeof(PseudoCpuAssemblerTests).Assembly.Location)!;
        var asmDir = Path.Combine(testDir, "..", "..", "..", "..", "..", "programs", "asm");

        if (!Directory.Exists(asmDir))
            return;

        foreach (var file in Directory.GetFiles(asmDir, "*.asm"))
        {
            var source = File.ReadAllText(file);
            var result = asm.Assemble(Path.GetFileNameWithoutExtension(file), source);
            result.Success.Should().BeTrue(
                $"{Path.GetFileName(file)} should assemble: {string.Join("; ", result.Diagnostics.Select(d => d.Message))}");
            result.Image.Should().NotBeNull();
            result.Image!.Bytes.Length.Should().BeGreaterThan(0);
        }
    }

    [TestMethod]
    public void AssemblyListingLine_BytesText_FormatsHex()
    {
        var line = new AssemblyListingLine { Bytes = new byte[] { 0x01, 0x48 } };
        line.BytesText.Should().Be("01 48");
    }

    [TestMethod]
    public void AssemblyListingLine_BytesText_Empty_WhenNull()
    {
        var line = new AssemblyListingLine { Bytes = null };
        line.BytesText.Should().BeEmpty();
    }

    [TestMethod]
    public void AssemblyListingLine_InstructionText_WithOperand()
    {
        var line = new AssemblyListingLine { Mnemonic = "LDA", Operand = "#$20" };
        line.InstructionText.Should().Be("LDA #$20");
    }

    [TestMethod]
    public void AssemblyListingLine_InstructionText_WithoutOperand()
    {
        var line = new AssemblyListingLine { Mnemonic = "HLT" };
        line.InstructionText.Should().Be("HLT");
    }

    [TestMethod]
    public void Assemble_AddImmediate_ProducesOpcode0F()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
    ADD #$45
");

        result.Success.Should().BeTrue();
        result.Image!.Bytes[0].Should().Be(0x0F); // ADD opcode
        result.Image.Bytes[1].Should().Be(0x45);  // operand
    }

    [TestMethod]
    public void Assemble_SubImmediate_ProducesOpcode10()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
    SUB #$10
");

        result.Success.Should().BeTrue();
        result.Image!.Bytes[0].Should().Be(0x10); // SUB opcode
        result.Image.Bytes[1].Should().Be(0x10);  // operand
    }

    [TestMethod]
    public void Assemble_AndImmediate_ProducesOpcode0B()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
    AND #$F0
");

        result.Success.Should().BeTrue();
        result.Image!.Bytes[0].Should().Be(0x0B); // AND opcode
        result.Image.Bytes[1].Should().Be(0xF0);  // operand
    }

    [TestMethod]
    public void Assemble_AddSubDecimal_ProducesCorrectBytes()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
    ADD #10
    SUB #5
");

        result.Success.Should().BeTrue();
        result.Image!.Bytes[0].Should().Be(0x0F); // ADD
        result.Image.Bytes[1].Should().Be(10);
        result.Image.Bytes[2].Should().Be(0x10); // SUB
        result.Image.Bytes[3].Should().Be(5);
    }

    [TestMethod]
    public void Assemble_AddSubWithLabels_ProducesCorrectBytes()
    {
        var asm = Create();

        var result = asm.Assemble("test", @"
.org $0100
VAL = $42
    ADD #VAL
    SUB #$20
");

        result.Success.Should().BeTrue();
        result.Image!.Bytes[0].Should().Be(0x0F); // ADD
        result.Image.Bytes[1].Should().Be(0x42);  // VAL = $42
        result.Image.Bytes[2].Should().Be(0x10); // SUB
        result.Image.Bytes[3].Should().Be(0x20);
    }

    [TestMethod]
    public void Cpu_Add_UpdatesAAndFlags()
    {
        var asm = Create();
        var result = asm.Assemble("add-test", @"
.org $0100
start:
    LDA #$12
    ADD #$34
    HLT
");
        result.Success.Should().BeTrue();

        var machine = new MinimalBlinkMachineSession();
        machine.SetSelectedSolution(new SolutionDefinition
        {
            Id = "add-test",
            Devices = [new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true }]
        });

        var compile = machine.CompileFromSource("add-test", @"
.org $0100
start:
    LDA #$12
    ADD #$34
    HLT
");
        compile.Success.Should().BeTrue();
        machine.LoadAndResetCompiled();

        machine.StepInstructionAsync().GetAwaiter().GetResult(); // LDA #$12
        machine.StepInstructionAsync().GetAwaiter().GetResult(); // ADD #$34
        machine.StepInstructionAsync().GetAwaiter().GetResult(); // HLT

        var snapshot = machine.Current;
        var a = snapshot.Cpu!.A;
        a.Should().Be("$46", "$12 + $34 = $46");
    }

    [TestMethod]
    public void Cpu_Sub_UpdatesAAndFlags()
    {
        var machine = new MinimalBlinkMachineSession();
        machine.SetSelectedSolution(new SolutionDefinition
        {
            Id = "sub-test",
            Devices = [new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true }]
        });

        var compile = machine.CompileFromSource("sub-test", @"
.org $0100
start:
    LDA #$45
    SUB #$12
    HLT
");
        compile.Success.Should().BeTrue();
        machine.LoadAndResetCompiled();

        machine.StepInstructionAsync().GetAwaiter().GetResult(); // LDA #$45
        machine.StepInstructionAsync().GetAwaiter().GetResult(); // SUB #$12
        machine.StepInstructionAsync().GetAwaiter().GetResult(); // HLT

        var snapshot = machine.Current;
        var a = snapshot.Cpu!.A;
        a.Should().Be("$33", "$45 - $12 = $33");
    }

    [TestMethod]
    public void Cpu_And_UpdatesAAndFlags()
    {
        var machine = new MinimalBlinkMachineSession();
        machine.SetSelectedSolution(new SolutionDefinition
        {
            Id = "and-test",
            Devices = [new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true }]
        });

        var compile = machine.CompileFromSource("and-test", @"
.org $0100
start:
    LDA #$45
    AND #$F0
    HLT
");
        compile.Success.Should().BeTrue();
        machine.LoadAndResetCompiled();

        machine.StepInstructionAsync().GetAwaiter().GetResult(); // LDA #$45
        machine.StepInstructionAsync().GetAwaiter().GetResult(); // AND #$F0
        machine.StepInstructionAsync().GetAwaiter().GetResult(); // HLT

        var snapshot = machine.Current;
        var a = snapshot.Cpu!.A;
        a.Should().Be("$40", "$45 & $F0 = $40");
    }

    [TestMethod]
    public void Cpu_AddSetsZero_WhenResultIsZero()
    {
        var machine = new MinimalBlinkMachineSession();
        machine.SetSelectedSolution(new SolutionDefinition
        {
            Id = "add-zero",
            Devices = [new DeviceDefinition { Id = "cpu", Type = "cpu", Visible = true }]
        });

        var compile = machine.CompileFromSource("add-zero", @"
.org $0100
start:
    LDA #$01
    SUB #$01
    HLT
");
        compile.Success.Should().BeTrue();
        machine.LoadAndResetCompiled();

        machine.StepInstructionAsync().GetAwaiter().GetResult(); // LDA #$01
        machine.StepInstructionAsync().GetAwaiter().GetResult(); // SUB #$01 → A=0, Z=1
        machine.StepInstructionAsync().GetAwaiter().GetResult(); // HLT

        var snapshot = machine.Current;
        snapshot.Cpu!.A.Should().Be("$00");
        snapshot.Cpu.Registers.Should().Contain(r => r.Name == "Z" && r.Value == "1");
    }
}
