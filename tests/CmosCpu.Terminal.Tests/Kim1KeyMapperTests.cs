using CmosCpu.Terminal.Tui;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class Kim1KeyMapperTests
{
    [TestMethod]
    public void MapToKim1Key_ReturnsNull_ForUnknownKey()
    {
        Kim1KeyMapper.MapToKim1Key("F12").Should().BeNull();
        Kim1KeyMapper.MapToKim1Key("Z").Should().BeNull();
        Kim1KeyMapper.MapToKim1Key("").Should().BeNull();
        Kim1KeyMapper.MapToKim1Key(null!).Should().BeNull();
    }

    [TestMethod]
    public void MapToKim1Key_MapsHexDigitKeys()
    {
        var keys = new (string key, string expected)[]
        {
            ("D0", "0"), ("D1", "1"), ("D2", "2"), ("D3", "3"),
            ("D4", "4"), ("D5", "5"), ("D6", "6"), ("D7", "7"),
            ("D8", "8"), ("D9", "9"),
            ("A", "A"), ("B", "B"), ("C", "C"),
            ("D", "D"), ("E", "E"), ("F", "F"),
        };

        foreach (var (key, expected) in keys)
            Kim1KeyMapper.MapToKim1Key(key).Should().Be(expected, $"key '{key}' should map to '{expected}'");
    }

    [TestMethod]
    public void MapToKim1Key_MapsSpecialKeys()
    {
        Kim1KeyMapper.MapToKim1Key("Enter").Should().Be("GO");
        Kim1KeyMapper.MapToKim1Key("Backspace").Should().Be("AD");
    }

    [TestMethod]
    public void MapToKim1Key_DoesNotMapNonHexLetters()
    {
        for (char c = 'G'; c <= 'Z'; c++)
            Kim1KeyMapper.MapToKim1Key(c.ToString()).Should().BeNull($"letter {c} should not map to KIM-1 key");
    }

    [TestMethod]
    public void MapToKim1Key_DoesNotMapNonHexDxKeys()
    {
        for (int i = 10; i <= 99; i++)
            Kim1KeyMapper.MapToKim1Key($"D{i}").Should().BeNull($"D{i} should not map");
    }

    [TestMethod]
    public void Map_ContainsAllExpectedMappings()
    {
        var map = Kim1KeyMapper.Map;
        map.Should().HaveCount(18);
        map["Enter"].Should().Be("GO");
        map["Backspace"].Should().Be("AD");
    }
}
