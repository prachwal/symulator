using CmosCpu.Core;
using FluentAssertions;

namespace CmosCpu.Core.Tests;

[TestClass]
public sealed class ComputerProfileTests
{
    private const string ValidJson = """
    {
        "id": "retro70-mos6502",
        "name": "Retro70 MOS 6502 Computer",
        "cpu": "mos6502",
        "clockHz": 1000000,
        "memory": {
            "ram": [{ "start": "0x0000", "size": "0x8000" }],
            "rom": [{ "start": "0xF000", "size": "0x1000" }],
            "vectors": { "reset": "0xF000", "nmi": "0xF100", "irq": "0xF200" }
        },
        "devices": [
            { "type": "text-display", "id": "screen", "start": "0xD000", "width": 40, "height": 25 },
            { "type": "keyboard", "id": "keyboard", "dataAddress": "0xD800", "statusAddress": "0xD801" }
        ]
    }
    """;

    [TestMethod]
    public void Load_ValidJson_ReturnsValidProfile()
    {
        var result = ComputerProfileLoader.Load(ValidJson);

        result.Errors.Should().BeEmpty();
        result.Profile.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public void Load_ValidJson_ParsesProfileProperties()
    {
        var result = ComputerProfileLoader.Load(ValidJson);

        result.Profile!.Id.Should().Be("retro70-mos6502");
        result.Profile.Name.Should().Be("Retro70 MOS 6502 Computer");
        result.Profile.Cpu.Should().Be("mos6502");
        result.Profile.ClockHz.Should().Be(1_000_000);
    }

    [TestMethod]
    public void Load_ValidJson_ParsesMemorySections()
    {
        var result = ComputerProfileLoader.Load(ValidJson);

        result.Profile!.Memory!.Ram.Should().HaveCount(1);
        result.Profile.Memory.Ram[0].Start.Should().Be("0x0000");
        result.Profile.Memory.Ram[0].Size.Should().Be("0x8000");

        result.Profile.Memory.Rom.Should().HaveCount(1);
        result.Profile.Memory.Rom[0].Start.Should().Be("0xF000");
        result.Profile.Memory.Rom[0].Size.Should().Be("0x1000");
    }

    [TestMethod]
    public void Load_ValidJson_ParsesVectors()
    {
        var result = ComputerProfileLoader.Load(ValidJson);

        result.Profile!.Memory!.Vectors.Should().NotBeNull();
        result.Profile.Memory.Vectors!.Reset.Should().Be("0xF000");
        result.Profile.Memory.Vectors.Nmi.Should().Be("0xF100");
        result.Profile.Memory.Vectors.Irq.Should().Be("0xF200");
    }

    [TestMethod]
    public void Load_ValidJson_ParsesDevices()
    {
        var result = ComputerProfileLoader.Load(ValidJson);

        result.Profile!.Devices.Should().HaveCount(2);
        result.Profile.Devices[0].Type.Should().Be("text-display");
        result.Profile.Devices[0].Id.Should().Be("screen");
        result.Profile.Devices[0].Width.Should().Be(40);
        result.Profile.Devices[0].Height.Should().Be(25);

        result.Profile.Devices[1].Type.Should().Be("keyboard");
        result.Profile.Devices[1].Id.Should().Be("keyboard");
        result.Profile.Devices[1].DataAddress.Should().Be("0xD800");
        result.Profile.Devices[1].StatusAddress.Should().Be("0xD801");
    }

    [TestMethod]
    public void Load_UnknownCpu_ReturnsError()
    {
        string json = ValidJson.Replace("mos6502", "z80");

        var result = ComputerProfileLoader.Load(json);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("z80"));
    }

    [TestMethod]
    public void Load_MissingId_ReturnsError()
    {
        string json = ValidJson.Replace("\"id\": \"retro70-mos6502\",", "");

        var result = ComputerProfileLoader.Load(json);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("id"));
    }

    [TestMethod]
    public void Load_ZeroClockHz_ReturnsError()
    {
        string json = ValidJson.Replace("1000000", "0");

        var result = ComputerProfileLoader.Load(json);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("clockHz"));
    }

    [TestMethod]
    public void Load_OverlappingMemoryRanges_ReturnsError()
    {
        string json = """
        {
            "id": "test",
            "name": "Test",
            "cpu": "mos6502",
            "clockHz": 1000000,
            "memory": {
                "ram": [{ "start": "0x0000", "size": "0x8000" }],
                "rom": [{ "start": "0x1000", "size": "0x1000" }]
            }
        }
        """;

        var result = ComputerProfileLoader.Load(json);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("overlap"));
    }

    [TestMethod]
    public void ParseHex_With0xPrefix_ReturnsCorrectValue()
    {
        ComputerProfileLoader.ParseHex("0x0000").Should().Be(0);
        ComputerProfileLoader.ParseHex("0xFFFF").Should().Be(65535);
        ComputerProfileLoader.ParseHex("0xD000").Should().Be(0xD000);
    }

    [TestMethod]
    public void ParseHex_WithDollarPrefix_ReturnsCorrectValue()
    {
        ComputerProfileLoader.ParseHex("$0000").Should().Be(0);
        ComputerProfileLoader.ParseHex("$FFFF").Should().Be(65535);
        ComputerProfileLoader.ParseHex("$D000").Should().Be(0xD000);
    }

    [TestMethod]
    public void ParseHex_BareHex_ReturnsCorrectValue()
    {
        ComputerProfileLoader.ParseHex("D000").Should().Be(0xD000);
        ComputerProfileLoader.ParseHex("FFFA").Should().Be(0xFFFA);
    }

    [TestMethod]
    public void Load_DevicesOverlappingWithMemory_ReturnsError()
    {
        string json = """
        {
            "id": "test",
            "name": "Test",
            "cpu": "mos6502",
            "clockHz": 1000000,
            "memory": {
                "ram": [{ "start": "0x0000", "size": "0xE000" }],
                "rom": [{ "start": "0xF000", "size": "0x1000" }]
            },
            "devices": [
                { "type": "text-display", "id": "screen", "start": "0xD000", "width": 40, "height": 25 }
            ]
        }
        """;

        var result = ComputerProfileLoader.Load(json);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("overlap"));
    }

    [TestMethod]
    public void Load_FileNotFound_ReturnsError()
    {
        var result = ComputerProfileLoader.LoadFromFile("nonexistent.json");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Failed to load"));
    }
}
