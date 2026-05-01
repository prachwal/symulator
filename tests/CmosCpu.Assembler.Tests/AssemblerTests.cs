using CmosCpu.Assembler;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CmosCpu.Assembler.Tests;

[TestClass]
public class AssemblerTests
{
    [TestMethod]
    public void Assemble_NOP_ProducesCorrectOpcode()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("NOP", 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x00);
    }

    [TestMethod]
    public void Assemble_LDA_IMM_ProducesCorrectBytes()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("LDA #0x42", 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x01);
        binary[1].Should().Be(0x42);
    }

    [TestMethod]
    public void Assemble_LDA_ABS_ProducesCorrectBytes()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("LDA 0x1234", 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x02);
        binary[1].Should().Be(0x34);
        binary[2].Should().Be(0x12);
    }

    [TestMethod]
    public void Assemble_STA_ABS_ProducesCorrectBytes()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("STA 0xC000", 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x03);
        binary[1].Should().Be(0x00);
        binary[2].Should().Be(0xC0);
    }

    [TestMethod]
    public void Assemble_ADD_IMM_ProducesCorrectBytes()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("ADD #0x10", 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x04);
        binary[1].Should().Be(0x10);
    }

    [TestMethod]
    public void Assemble_SUB_IMM_ProducesCorrectBytes()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("SUB #0x01", 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x05);
        binary[1].Should().Be(0x01);
    }

    [TestMethod]
    public void Assemble_JMP_ProducesCorrectBytes()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("JMP 0x9000", 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x06);
        binary[1].Should().Be(0x00);
        binary[2].Should().Be(0x90);
    }

    [TestMethod]
    public void Assemble_CALL_ProducesCorrectBytes()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("CALL 0x9000", 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x0F);
        binary[1].Should().Be(0x00);
        binary[2].Should().Be(0x90);
    }

    [TestMethod]
    public void Assemble_HLT_ProducesCorrectOpcode()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("HLT", 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x11);
    }

    [TestMethod]
    public void Assemble_WithLabel_ResolvesAddress()
    {
        string source = @"
.org 0x8000
start:
    LDA #0x01
    JMP start
";
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble(source, 0x8000);
        errors.Should().BeEmpty();
        binary[0].Should().Be(0x01);
        binary[1].Should().Be(0x01);
        binary[2].Should().Be(0x06);
        binary[3].Should().Be(0x00);
        binary[4].Should().Be(0x80);
    }

    [TestMethod]
    public void Assemble_WithDotOrg_SetsOrigin()
    {
        string source = @"
.org 0x9000
    LDA #0x10
";
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble(source, 0x8000);
        errors.Should().BeEmpty();
        binary.Length.Should().Be(2);
        binary[0].Should().Be(0x01);
        binary[1].Should().Be(0x10);
    }

    [TestMethod]
    public void Assemble_WithComments_IgnoresComments()
    {
        string source = "LDA #0x42 ; this is a comment";
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble(source, 0x8000);
        errors.Should().BeEmpty();
        binary.Length.Should().Be(2);
        binary[0].Should().Be(0x01);
        binary[1].Should().Be(0x42);
    }

    [TestMethod]
    public void Assemble_InvalidMnemonic_ReturnsError()
    {
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble("XYZ 0x42", 0x8000);
        errors.Should().NotBeEmpty();
    }

    [TestMethod]
    public void Assemble_FullBlinkProgram_ProducesCorrectLength()
    {
        string source = @"
.org 0x8000
start:
    LDA #0x01
    STA 0xC000
    CALL delay
    LDA #0x00
    STA 0xC000
    CALL delay
    JMP start
delay:
    LDA #0xFF
loop:
    SUB #0x01
    JNZ loop
    RET
";
        var asm = new SimpleAssembler();
        var (binary, errors) = asm.Assemble(source, 0x8000);
        errors.Should().BeEmpty();
        binary.Length.Should().BeGreaterThan(0);
    }
}
