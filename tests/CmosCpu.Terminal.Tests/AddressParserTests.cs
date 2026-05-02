using CmosCpu.Terminal;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class AddressParserTests
{
    [TestMethod]
    public void Parse_ShouldAcceptHexWith0xPrefix()
    {
        AddressParser.Parse("0xE000").Should().Be(0xE000);
    }

    [TestMethod]
    public void Parse_ShouldAcceptDollarPrefix()
    {
        AddressParser.Parse("$E000").Should().Be(0xE000);
    }

    [TestMethod]
    public void Parse_ShouldAcceptBareHex()
    {
        AddressParser.Parse("E000").Should().Be(0xE000);
    }

    [TestMethod]
    public void Parse_ShouldAcceptDecimal()
    {
        AddressParser.Parse("57344").Should().Be(0xE000);
    }

    [TestMethod]
    public void Parse_ShouldHandleLowercaseHex()
    {
        AddressParser.Parse("0xe000").Should().Be(0xE000);
    }

    [TestMethod]
    public void Parse_ShouldHandleZero()
    {
        AddressParser.Parse("0x0000").Should().Be(0);
    }

    [TestMethod]
    public void Parse_ShouldHandleMaxAddress()
    {
        AddressParser.Parse("0xFFFF").Should().Be(0xFFFF);
    }

    [TestMethod]
    public void TryParse_Invalid_ReturnsFalse()
    {
        AddressParser.TryParse("xyz", out _).Should().BeFalse();
    }

    [TestMethod]
    public void TryParse_Empty_ReturnsFalse()
    {
        AddressParser.TryParse("", out _).Should().BeFalse();
    }

    [TestMethod]
    public void TryParse_Valid_ReturnsTrue()
    {
        AddressParser.TryParse("0x1000", out ushort addr).Should().BeTrue();
        addr.Should().Be(0x1000);
    }
}
