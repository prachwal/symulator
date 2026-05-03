using CmosCpu.Computer.Devices.Serial;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class UartDeviceTests
{
    [TestMethod]
    public void WriteData_ShouldRaiseByteTransmitted()
    {
        var uart = new UartDevice();
        byte? transmitted = null;
        uart.ByteTransmitted += (_, args) => transmitted = args.Value;

        uart.WriteData((byte)'A');

        transmitted.Should().Be((byte)'A');
    }

    [TestMethod]
    public void ReceiveFromTerminal_ShouldSetRxReady()
    {
        var uart = new UartDevice();
        uart.ReceiveFromTerminal((byte)'A');
        (uart.ReadStatus() & UartDevice.StatusRxReady).Should().NotBe(0);
    }

    [TestMethod]
    public void ReadData_ShouldReturnReceivedByte()
    {
        var uart = new UartDevice();
        uart.ReceiveFromTerminal((byte)'A');
        uart.ReadData().Should().Be((byte)'A');
    }

    [TestMethod]
    public void ReadData_ShouldClearRxReady_WhenLastByteRead()
    {
        var uart = new UartDevice();
        uart.ReceiveFromTerminal((byte)'A');
        uart.ReadData();
        (uart.ReadStatus() & UartDevice.StatusRxReady).Should().Be(0);
    }

    [TestMethod]
    public void ReceiveFromTerminal_ShouldSetOverrun_WhenRxFifoFull()
    {
        var uart = new UartDevice(rxCapacity: 1);
        uart.ReceiveFromTerminal((byte)'A');
        uart.ReceiveFromTerminal((byte)'B');
        (uart.ReadStatus() & UartDevice.StatusOverrun).Should().NotBe(0);
    }

    [TestMethod]
    public void Status_ShouldShowTxReadyAfterReset()
    {
        var uart = new UartDevice();
        byte status = uart.ReadStatus();
        (status & UartDevice.StatusTxReady).Should().NotBe(0);
        (status & UartDevice.StatusTxEmpty).Should().NotBe(0);
    }

    [TestMethod]
    public void WriteControl_ShouldToggleRxTx()
    {
        var uart = new UartDevice();
        uart.WriteControl(0x00); // disable both
        var snap = uart.GetSnapshot();
        snap.RxEnabled.Should().BeFalse();
        snap.TxEnabled.Should().BeFalse();

        uart.WriteControl(0x03); // enable both
        snap = uart.GetSnapshot();
        snap.RxEnabled.Should().BeTrue();
        snap.TxEnabled.Should().BeTrue();
    }
}

[TestClass]
public sealed class MemoryMappedUartAdapterTests
{
    [TestMethod]
    public void WriteData_ShouldTransmitByte()
    {
        var uart = new UartDevice();
        var adapter = new MemoryMappedUartAdapter(uart);
        byte? transmitted = null;
        uart.ByteTransmitted += (_, args) => transmitted = args.Value;

        adapter.Write(0, (byte)'Z');

        transmitted.Should().Be((byte)'Z');
    }

    [TestMethod]
    public void ReadStatus_ShouldReturnTxReady()
    {
        var uart = new UartDevice();
        var adapter = new MemoryMappedUartAdapter(uart);
        byte status = adapter.Read(1);
        (status & UartDevice.StatusTxReady).Should().NotBe(0);
    }

    [TestMethod]
    public void ReadData_ShouldReturnReceivedByte()
    {
        var uart = new UartDevice();
        var adapter = new MemoryMappedUartAdapter(uart);
        uart.ReceiveFromTerminal((byte)'X');
        adapter.Read(0).Should().Be((byte)'X');
    }
}
