using CmosCpu.Computer;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class ComputerMemoryBusMirrorTests
{
    [TestMethod]
    public void MapRomMirror_ShouldShareData()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRom(0x1C00, 0x0400);
        bus.LoadRom(0x1C00, [0x11, 0x22, 0x33, 0x44, 0x55]);

        bus.MapRomMirror(0x1C00, 0xFC00, 0x0400);

        bus.ReadByte(0xFC00).Should().Be(0x11);
        bus.ReadByte(0xFC01).Should().Be(0x22);
        bus.ReadByte(0xFC04).Should().Be(0x55);
    }

    [TestMethod]
    public void MapRomMirror_VectorArea_ShouldReflectSource()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRom(0x1C00, 0x0400);

        byte[] romData = new byte[0x0400];
        romData[0x3FC] = 0x22; // 0x1FFC -> mirrored to 0xFFFC
        romData[0x3FD] = 0x1C; // 0x1FFD -> mirrored to 0xFFFD
        bus.LoadRom(0x1C00, romData);

        bus.MapRomMirror(0x1C00, 0xFC00, 0x0400);

        bus.ReadByte(0xFFFC).Should().Be(0x22);
        bus.ReadByte(0xFFFD).Should().Be(0x1C);
    }

    [TestMethod]
    public void MapRomMirror_PartialRange_ShouldWork()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRom(0x1C00, 0x0400);
        bus.LoadRom(0x1C00, [0xAA, 0xBB, 0xCC]);

        bus.MapRomMirror(0x1C00, 0xFC00, 3);

        bus.ReadByte(0xFC00).Should().Be(0xAA);
        bus.ReadByte(0xFC02).Should().Be(0xCC);
    }

    [TestMethod]
    public void MapRomMirror_WriteIsNoOp_ReadRemainsUnchanged()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRom(0x1C00, 0x0400);
        bus.LoadRom(0x1C00, [0x42]);
        bus.MapRomMirror(0x1C00, 0xFC00, 0x0400);

        bus.WriteByte(0xFC00, 0xFF);

        bus.ReadByte(0xFC00).Should().Be(0x42);
        bus.ReadByte(0x1C00).Should().Be(0x42);
    }

    [TestMethod]
    public void MapRomMirror_NoSource_DoesNotThrow()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRom(0x1000, 0x0100);

        bus.MapRomMirror(0x1C00, 0xFC00, 0x0400);
    }
}
