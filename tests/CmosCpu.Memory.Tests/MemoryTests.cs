using CmosCpu.Core;
using CmosCpu.Memory;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CmosCpu.Memory.Tests;

[TestClass]
public class MemoryTests
{
    [TestMethod]
    public void RamDevice_WriteThenRead_ReturnsValue()
    {
        var ram = new RamDevice(0x0000, 0x7FFF);
        ram.Write(0x1000, 0xAB);
        byte value = ram.Read(0x1000);
        value.Should().Be(0xAB);
    }

    [TestMethod]
    public void RamDevice_ReadUninitialized_ReturnsZero()
    {
        var ram = new RamDevice(0x0000, 0x7FFF);
        byte value = ram.Read(0x2000);
        value.Should().Be(0);
    }

    [TestMethod]
    public void RomDevice_Write_DoesNotChangeValue()
    {
        var rom = new RomDevice(0x8000, 0xBFFF);
        rom.Read(0x8000).Should().Be(0);
        rom.Write(0x8000, 0xFF);
        rom.Read(0x8000).Should().Be(0);
    }

    [TestMethod]
    public void RomDevice_Load_ThenReadReturnsData()
    {
        var rom = new RomDevice(0x8000, 0xBFFF);
        byte[] program = { 0x01, 0x02, 0x03, 0x04 };
        rom.Load(program, 0);
        rom.Read(0x8000).Should().Be(0x01);
        rom.Read(0x8001).Should().Be(0x02);
        rom.Read(0x8002).Should().Be(0x03);
        rom.Read(0x8003).Should().Be(0x04);
    }

    [TestMethod]
    public void Contains_AddressInRange_ReturnsTrue()
    {
        var ram = new RamDevice(0x0000, 0x7FFF);
        ram.Contains(0x0000).Should().BeTrue();
        ram.Contains(0x7FFF).Should().BeTrue();
        ram.Contains(0x4000).Should().BeTrue();
    }

    [TestMethod]
    public void Contains_AddressOutOfRange_ReturnsFalse()
    {
        var ram = new RamDevice(0x0000, 0x7FFF);
        ram.Contains(0x8000).Should().BeFalse();
        ram.Contains(0xFFFF).Should().BeFalse();
    }

    [TestMethod]
    public void Dump_ReturnsCorrectRange()
    {
        var ram = new RamDevice(0x0000, 0x7FFF);
        ram.Write(0x0010, 0xAA);
        ram.Write(0x0011, 0xBB);
        byte[] dump = ram.Dump(0x0010, 0x0011);
        dump.Length.Should().Be(2);
        dump[0].Should().Be(0xAA);
        dump[1].Should().Be(0xBB);
    }

    [TestMethod]
    public void MemoryMap_ValidRegions_CreatesSuccessfully()
    {
        var regions = new[]
        {
            new MemoryRegion { Name = "RAM", Start = 0x0000, End = 0x7FFF, DeviceName = "RamDevice" },
            new MemoryRegion { Name = "ROM", Start = 0x8000, End = 0xBFFF, DeviceName = "RomDevice" },
        };

        var map = new MemoryMap(regions);

        map.Regions.Should().HaveCount(2);
    }

    [TestMethod]
    public void MemoryMap_StartGreaterThanEnd_Throws()
    {
        var regions = new[]
        {
            new MemoryRegion { Name = "Bad", Start = 0x8000, End = 0x7FFF, DeviceName = "BadDevice" },
        };

        Action act = () => new MemoryMap(regions);
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void MemoryMap_OverlappingRegions_Throws()
    {
        var regions = new[]
        {
            new MemoryRegion { Name = "A", Start = 0x0000, End = 0x7FFF, DeviceName = "DevA" },
            new MemoryRegion { Name = "B", Start = 0x4000, End = 0xBFFF, DeviceName = "DevB" },
        };

        Action act = () => new MemoryMap(regions);
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void MemoryMap_FindRegion_ReturnsCorrectRegion()
    {
        var regions = new[]
        {
            new MemoryRegion { Name = "RAM", Start = 0x0000, End = 0x7FFF, DeviceName = "RamDevice" },
            new MemoryRegion { Name = "ROM", Start = 0x8000, End = 0xBFFF, DeviceName = "RomDevice" },
        };

        var map = new MemoryMap(regions);

        map.FindRegion(0x0000)?.Name.Should().Be("RAM");
        map.FindRegion(0x7FFF)?.Name.Should().Be("RAM");
        map.FindRegion(0x8000)?.Name.Should().Be("ROM");
        map.FindRegion(0xBFFF)?.Name.Should().Be("ROM");
    }

    [TestMethod]
    public void MemoryMap_FindRegion_ReturnsNullForUnmapped()
    {
        var regions = new[]
        {
            new MemoryRegion { Name = "RAM", Start = 0x0000, End = 0x7FFF, DeviceName = "RamDevice" },
        };

        var map = new MemoryMap(regions);

        map.FindRegion(0x8000).Should().BeNull();
        map.FindRegion(0xFFFF).Should().BeNull();
    }
}
