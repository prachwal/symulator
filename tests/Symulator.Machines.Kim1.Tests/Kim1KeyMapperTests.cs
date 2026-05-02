using FluentAssertions;
using Symulator.Machines.Kim1.Services;

namespace Symulator.Machines.Kim1.Tests;

[TestClass]
public sealed class Kim1KeyMapperTests
{
    [TestMethod]
    public void MapToKim1Key_HexDigit_ReturnsMappedValue()
    {
        Kim1KeyMapper.MapToKim1Key("D0").Should().Be("0");
        Kim1KeyMapper.MapToKim1Key("D9").Should().Be("9");
        Kim1KeyMapper.MapToKim1Key("A").Should().Be("A");
        Kim1KeyMapper.MapToKim1Key("F").Should().Be("F");
    }

    [TestMethod]
    public void MapToKim1Key_Enter_ReturnsGO()
    {
        Kim1KeyMapper.MapToKim1Key("Enter").Should().Be("GO");
    }

    [TestMethod]
    public void MapToKim1Key_Backspace_ReturnsAD()
    {
        Kim1KeyMapper.MapToKim1Key("Backspace").Should().Be("AD");
    }

    [TestMethod]
    public void MapToKim1Key_UnknownKey_ReturnsNull()
    {
        Kim1KeyMapper.MapToKim1Key("Z").Should().BeNull();
    }

    [TestMethod]
    public void MapToKim1Key_Empty_ReturnsNull()
    {
        Kim1KeyMapper.MapToKim1Key("").Should().BeNull();
        Kim1KeyMapper.MapToKim1Key(null!).Should().BeNull();
    }

    [TestMethod]
    public void Map_ContainsAllExpectedKeys()
    {
        Kim1KeyMapper.Map.Should().ContainKeys("D0", "D1", "D9", "A", "F", "Enter", "Backspace");
        Kim1KeyMapper.Map.Should().HaveCount(18);
    }
}
