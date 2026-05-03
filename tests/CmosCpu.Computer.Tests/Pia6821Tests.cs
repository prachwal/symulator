using CmosCpu.Computer.Devices;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class Pia6821Tests
{
    [TestMethod]
    public void Reset_PortsAreInputs_DdrIsZero()
    {
        var pia = new Pia6821();

        pia.Reset();

        // After reset, bit2=0 => reading DDR
        pia.Read(pia.StartAddress).Should().Be(0, "DDRA should be 0 after reset");
        pia.Read((ushort)(pia.StartAddress + 2)).Should().Be(0, "DDRB should be 0 after reset");
    }

    [TestMethod]
    public void WriteOffset0_Bit2Control0_ChangesDDRA()
    {
        var pia = new Pia6821();
        ushort addr = pia.StartAddress;

        pia.Write(addr, 0x42); // bit2=0, so writes to DDRA
        byte ddr = pia.Read(addr); // bit2=0 reads DDR
        ddr.Should().Be(0x42);
    }

    [TestMethod]
    public void WriteOffset2_Bit2Control0_ChangesDDRB()
    {
        var pia = new Pia6821();
        ushort addr = pia.StartAddress;

        pia.Write((ushort)(addr + 2), 0xA5);
        byte ddr = pia.Read((ushort)(addr + 2));
        ddr.Should().Be(0xA5);
    }

    [TestMethod]
    public void WriteOffset0_Bit2Control1_ChangesDataA()
    {
        var pia = new Pia6821();
        ushort addr = pia.StartAddress;

        pia.Write((ushort)(addr + 1), 0x04); // CRA bit2=1 => data register
        pia.PortAInput = 0xFF; // all input pins high
        pia.Write(addr, 0x7B);

        byte data = pia.Read(addr); // reads data: (data&ddr)|(input&~ddr) = (7B&00)|(FF&FF) = FF
        data.Should().Be(0xFF);
    }

    [TestMethod]
    public void WriteOffset2_Bit2Control1_ChangesDataB()
    {
        var pia = new Pia6821();
        ushort addr = pia.StartAddress;

        pia.Write((ushort)(addr + 3), 0x04); // CRB bit2=1
        pia.PortBInput = 0xFF; // all input pins high
        pia.Write((ushort)(addr + 2), 0xB4);

        byte data = pia.Read((ushort)(addr + 2));
        data.Should().Be(0xFF, "DDRB=0 so output contributes 0, input FF → FF");
    }

    [TestMethod]
    public void ReadData_MixesOutputBitsAndInputPins()
    {
        var pia = new Pia6821();
        ushort addr = pia.StartAddress;

        pia.Write(addr, 0x0F); // DDRA = lower nibble output (bit2=0 → DDR)

        pia.Write((ushort)(addr + 1), 0x04); // CRA bit2=1 → data mode

        pia.Write(addr, 0x05); // data = 0000 0101

        pia.PortAInput = 0xF0; // input pins = 1111 0000

        byte result = pia.Read(addr);
        result.Should().Be(0xF5, "output bits (05&0F=05) + input bits (F0&F0=F0) = F5");
    }

    [TestMethod]
    public void OutputChanged_FiresOnDataChange()
    {
        var pia = new Pia6821();
        ushort addr = pia.StartAddress;
        byte changedValue = 0;

        pia.PortAOutputChanged += v => changedValue = v;

        // Set DDRA = $FF (all outputs) via DDR mode (CRA bit2=0)
        pia.Write(addr, 0xFF);

        // Switch to data mode (CRA bit2=1), read to clear flag
        pia.Write((ushort)(addr + 1), 0x04);

        // Write new data — should trigger event
        pia.Write(addr, 0x55);

        changedValue.Should().Be(0x55);
    }

    [TestMethod]
    public void OffsetsAreMaskedTo4Registers()
    {
        var pia = new Pia6821(0xD000, 0xD0FF);
        ushort addr = pia.StartAddress;

        // Write CRA = 0x02 via offset 1
        pia.Write((ushort)(addr + 1), 0x02);
        // Write to offset 5 (= offset 1 after mask) → should write CRA again
        pia.Write((ushort)(addr + 5), 0x06);
        // Write to offset 9 (= offset 1 after mask) → should write CRA again
        pia.Write((ushort)(addr + 9), 0x0A);

        // Read CRA via offset 1 → should be 0x0A (last write)
        pia.Read((ushort)(addr + 1)).Should().Be(0x0A, "offset 5 and 9 alias to offset 1");

        // Read CRA via offset 5 → should also be 0x0A
        pia.Read((ushort)(addr + 5)).Should().Be(0x0A, "offset 5 reads same as offset 1");
    }

    [TestMethod]
    public void Ca1RisingEdge_SetsStatusAndIrq()
    {
        var pia = new Pia6821();
        ushort addr = pia.StartAddress;

        pia.SetCa1(false);
        pia.SetCa1(true);

        pia.Read((ushort)(addr + 1)).Should().Be(0x80, "CA1 sets IRQ flag A (bit7=1)");
        pia.IrqActive.Should().BeFalse("IRQ enable bit1 not set");
    }

    [TestMethod]
    public void Cb1RisingEdge_SetsStatusAndIrq()
    {
        var pia = new Pia6821();
        ushort addr = pia.StartAddress;

        pia.SetCb1(false);
        pia.SetCb1(true);

        pia.Read((ushort)(addr + 3)).Should().Be(0x80, "CB1 sets IRQ flag B (bit7=1)");
        pia.IrqActive.Should().BeFalse("IRQ enable bit1 not set");
    }

    [TestMethod]
    public void IrqActive_CombinesBothPorts()
    {
        var pia = new Pia6821();
        ushort addr = pia.StartAddress;

        // Set CA1 IRQ enable (bit1=1) then fire CA1 (bit7=1)
        pia.Write((ushort)(addr + 1), 0x02);
        pia.SetCa1(true);
        pia.IrqActive.Should().BeTrue();
    }
}
