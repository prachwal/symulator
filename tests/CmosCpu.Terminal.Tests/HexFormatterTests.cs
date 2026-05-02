using CmosCpu.Terminal;
using FluentAssertions;

namespace CmosCpu.Terminal.Tests;

[TestClass]
public sealed class HexFormatterTests
{
    [TestMethod]
    public void FormatHexDump_ShouldProduceCorrectOutput()
    {
        byte[] data = [0xA9, 0x00, 0x8D, 0x00, 0xD0, 0xEA, 0xEA, 0xEA, 0x20, 0x00, 0xE0, 0x4C, 0x00, 0xE0, 0x00, 0x00];
        string result = HexFormatter.FormatHexDump(data, 0xE000);

        result.Should().Contain("E000:");
        result.Should().Contain("A9 00 8D 00 D0 EA EA EA");
        result.Should().Contain("20 00 E0 4C 00 E0 00 00");
        result.Should().Contain("|........ ..L....|");
    }

    [TestMethod]
    public void FormatHexDump_ShouldHandleEmptyData()
    {
        string result = HexFormatter.FormatHexDump([], 0x0000);
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void FormatHexDump_ShouldHandlePartialRow()
    {
        byte[] data = [0x01, 0x02, 0x03];
        string result = HexFormatter.FormatHexDump(data, 0x1000);

        result.Should().Contain("1000:");
        result.Should().Contain("01 02 03");
    }

    [TestMethod]
    public void FormatHexDump_ShouldHandleNonZeroStart()
    {
        byte[] data = [0xFF, 0xFE];
        string result = HexFormatter.FormatHexDump(data, 0x8000);

        result.Should().Contain("8000:");
        result.Should().Contain("FF FE");
    }

    [TestMethod]
    public void FormatHexDump_AsciiSection_ShowsDotForNonPrintable()
    {
        byte[] data = [0x00, 0x01, 0x1F, 0x7F, 0x41];
        string result = HexFormatter.FormatHexDump(data, 0x0000, 8);

        result.Should().Contain("|....A|");
    }

    [TestMethod]
    public void FormatStack_DelegatesToFormatHexDump()
    {
        byte[] data = [0x01, 0x02];
        string result = HexFormatter.FormatStack(data, 0x0100);

        result.Should().Contain("0100:");
    }

    [TestMethod]
    public void FormatHexDump_MidpointSpacing_After8Bytes()
    {
        byte[] data = new byte[16];
        for (int i = 0; i < 16; i++) data[i] = (byte)i;

        string result = HexFormatter.FormatHexDump(data, 0x0000);

        result.Should().Contain("07  08");
    }
}
