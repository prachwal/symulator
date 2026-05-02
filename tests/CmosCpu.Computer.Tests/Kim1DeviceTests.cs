using CmosCpu.Computer;
using CmosCpu.Core;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class Kim1DeviceTests
{
    internal const string Kim1Profile = """
    {
        "id": "kim-1",
        "name": "KIM-1",
        "cpu": "mos6502",
        "clockHz": 1000000,
        "memory": {
            "ram": [{ "start": "0x0000", "size": "0x0400" }],
            "rom": [
                { "start": "0x1800", "size": "0x0400" },
                { "start": "0x1C00", "size": "0x0400" }
            ],
            "vectors": { "reset": "0x1C00", "nmi": "0x1C00", "irq": "0x1C00" }
        },
        "devices": [
            { "type": "kim1-6530-io", "id": "riot6530", "start": "0x1700", "size": "0x0100" },
            { "type": "kim1-led-display", "id": "display", "digits": 6 },
            { "type": "kim1-keypad", "id": "keypad" }
        ]
    }
    """;

    [TestMethod]
    public void Kim1Io_Handles_1700_To_17FF()
    {
        var riot = new Kim1Riot6530IoDevice();

        riot.Handles(0x1700).Should().BeTrue();
        riot.Handles(0x17FF).Should().BeTrue();
        riot.Handles(0x16FF).Should().BeFalse();
        riot.Handles(0x1800).Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Io_PortA_WithDDR_ReturnsWrittenValue()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1701, 0xFF); // DDRA = all outputs
        riot.Write(0x1700, 0xAB);

        riot.Read(0x1700).Should().Be(0xAB);
    }

    [TestMethod]
    public void Kim1Io_DDR_RegisterRoundtrip()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1701, 0xAB);
        riot.Read(0x1701).Should().Be(0xAB);
    }

    [TestMethod]
    public void Kim1Io_Version_Increments_OnWriteChange()
    {
        var riot = new Kim1Riot6530IoDevice();

        long v0 = riot.Version;
        riot.Write(0x1701, 0x42);
        riot.Version.Should().Be(v0 + 1);

        riot.Write(0x1701, 0x42);
        riot.Version.Should().Be(v0 + 1);

        riot.Write(0x1701, 0xFF);
        riot.Version.Should().Be(v0 + 2);
    }

    [TestMethod]
    public void Kim1Io_PortA_InputBitsComeFromExternalInput()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1701, 0xF0); // upper nibble output, lower nibble input
        riot.Write(0x1700, 0xFF); // output latch = all ones
        riot.SetPortAInput(0x0A); // lower input bits = 1010

        byte result = riot.Read(0x1700);
        result.Should().Be(0xFA); // upper = latch(FF), lower = input(0A)
    }

    [TestMethod]
    public void Kim1Io_PortB_WithDDR_ReturnsWrittenValue()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1703, 0xFF); // DDRB = all outputs
        riot.Write(0x1702, 0xCD);

        riot.Read(0x1702).Should().Be(0xCD);
    }

    [TestMethod]
    public void Kim1Io_PortB_MixedDDR()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1703, 0x0F); // lower nibble output, upper nibble input
        riot.Write(0x1702, 0xFF); // output latch = all ones
        riot.SetPortBInput(0xB0); // upper input bits = 1011

        byte result = riot.Read(0x1702);
        result.Should().Be(0xBF); // upper = input(B0), lower = latch(FF)
    }

    [TestMethod]
    public void Kim1Io_TimerWritePrescaler1_TicksCorrectly()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1704, 10); // load timer = 10, prescaler /1

        riot.Tick(5);
        riot.TimerValue.Should().Be(5);

        riot.Tick(5);
        riot.TimerValue.Should().Be(0);
        riot.TimerUnderflow.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerPrescaler8_TicksCorrectly()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1705, 5); // load timer = 5, prescaler /8

        riot.Tick(40); // 5 * 8 = 40 cycles to underflow
        riot.TimerValue.Should().Be(0);
        riot.TimerUnderflow.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerPrescaler64_TicksCorrectly()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1706, 3); // load timer, prescaler /64

        riot.Tick(192); // 3 * 64 = 192 cycles
        riot.TimerValue.Should().Be(0);
        riot.TimerUnderflow.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerPrescaler1024_TicksCorrectly()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1707, 2); // load timer, prescaler /1024

        riot.Tick(2048); // 2 * 1024 = 2048 cycles
        riot.TimerValue.Should().Be(0);
        riot.TimerUnderflow.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerUnderflow_SetsIrqPending_WhenIrqEnabled()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 5); // load timer=5, prescaler /1, IRQ enabled

        riot.Tick(5);
        riot.TimerUnderflow.Should().BeTrue();
        riot.IrqPending.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_TimerUnderflow_DoesNotSetIrqPending_WhenIrqDisabled()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1704, 5); // load timer=5, prescaler /1, IRQ NOT enabled

        riot.Tick(5);
        riot.TimerUnderflow.Should().BeTrue();
        riot.IrqPending.Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Io_ReadTimerFlagRegister_ClearsUnderflowAndIrq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 5);
        riot.Tick(5);
        riot.TimerUnderflow.Should().BeTrue();

        riot.Read(0x1705);
        riot.TimerUnderflow.Should().BeFalse();
        riot.IrqPending.Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Io_TimerWrite_AfterUnderflow_RestartsTimer()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1704, 2);
        riot.Tick(2);
        riot.TimerUnderflow.Should().BeTrue();

        riot.Write(0x1704, 10);
        riot.TimerUnderflow.Should().BeFalse();
        riot.TimerValue.Should().Be(10);
    }

    [TestMethod]
    public void Kim1Io_IrqEnable_WriteTo08_EnablesIrq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.TimerIrqEnabled.Should().BeFalse();

        riot.Write(0x1708, 10);
        riot.TimerIrqEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Io_IrqDisable_WriteTo09_DisablesIrq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 10);
        riot.TimerIrqEnabled.Should().BeTrue();

        riot.Write(0x1709, 0);
        riot.TimerIrqEnabled.Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Keypad_PressKey_AddsKey()
    {
        var keypad = new Kim1KeypadState();

        keypad.PressKey("A");

        keypad.PressedKeys.Should().Contain("A");
        keypad.PressedKeys.Count.Should().Be(1);
    }

    [TestMethod]
    public void Kim1Keypad_ReleaseKey_RemovesKey()
    {
        var keypad = new Kim1KeypadState();

        keypad.PressKey("A");
        keypad.ReleaseKey("A");

        keypad.PressedKeys.Should().NotContain("A");
        keypad.PressedKeys.Should().BeEmpty();
    }

    [TestMethod]
    public void Kim1LedDisplay_DefaultState_IsSixDashes()
    {
        var display = new Kim1LedDisplayState();

        display.Digits.Should().Be("------");
    }

    [TestMethod]
    public void Kim1Profile_LoadsWithoutUnknownDeviceException()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Kim1Profile);

        machine.Should().NotBeNull();
        machine.Profile.Id.Should().Be("kim-1");
        machine.Kim1Riot.Should().NotBeNull();
        machine.Kim1Riot003.Should().BeNull();
        machine.Kim1LedDisplay.Should().NotBeNull();
        machine.Kim1Keypad.Should().NotBeNull();
    }

    [TestMethod]
    public void ComputerMachine_MapsKim1IoRange()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Kim1Profile);

        machine.Kim1Riot!.Write(0x1701, 0xFF); // DDRA all outputs
        machine.Memory.WriteByte(0x1700, 0x55);

        machine.Memory.ReadByte(0x1700).Should().Be(0x55);

        machine.Kim1Riot.Write(0x1703, 0xFF); // DDRB all outputs
        machine.Memory.WriteByte(0x1702, 0xAA);
        machine.Memory.ReadByte(0x1702).Should().Be(0xAA);
    }

    [TestMethod]
    public void Kim1Keypad_OnlyValidKeysAccepted()
    {
        var keypad = new Kim1KeypadState();

        keypad.PressKey("AD");
        keypad.PressKey("GO");
        keypad.PressKey("INVALID");

        keypad.PressedKeys.Should().Contain("AD");
        keypad.PressedKeys.Should().Contain("GO");
        keypad.PressedKeys.Count.Should().Be(2);
    }

    [TestMethod]
    public void Kim1LedDisplay_Update_ChangesDigits()
    {
        var display = new Kim1LedDisplayState();

        display.Update("00FF00");

        display.Digits.Should().Be("00FF00");
    }

    // --- Phase 1: Internal RAM ---

    [TestMethod]
    public void Kim1Riot6530Ram_WriteRead_002()
    {
        var riot = new Kim1Riot6530IoDevice(0x1700, 0x17FF);
        riot.Write(0x1740, 0xAB);
        riot.Write(0x17BF, 0xCD);

        riot.Read(0x1740).Should().Be(0xAB);
        riot.Read(0x17BF).Should().Be(0xCD);
    }

    [TestMethod]
    public void Kim1Riot6530Ram_WriteRead_003()
    {
        var riot = new Kim1Riot6530IoDevice(0x1400, 0x14FF);
        riot.Write(0x1440, 0x12);
        riot.Write(0x14BF, 0x34);

        riot.Read(0x1440).Should().Be(0x12);
        riot.Read(0x14BF).Should().Be(0x34);
    }

    [TestMethod]
    public void Kim1Riot6530Ram_DoesNotAffectPorts()
    {
        var riot = new Kim1Riot6530IoDevice(0x1700, 0x17FF);
        riot.Write(0x1740, 0xFF);
        riot.Write(0x1741, 0xFF);

        riot.Write(0x1701, 0xFF); // DDRA all outputs
        riot.Write(0x1700, 0x00);
        riot.Read(0x1700).Should().Be(0x00);
        riot.Read(0x1740).Should().Be(0xFF);
    }

    // --- Phase 2: Keypad matrix ---

    [TestMethod]
    public void Kim1KeypadMatrix_PressedKeyVisibleOnSelectedColumn()
    {
        var keypad = new Kim1KeypadState();
        keypad.PressKey("5");

        byte rowBits = keypad.GetRowState(0xFD, 0xFF); // column 1 active, DDR = all outputs
        rowBits.Should().Be(0x0D); // row 1 (bit 1) = 0
    }

    [TestMethod]
    public void Kim1KeypadMatrix_KeyNotVisibleOnWrongColumn()
    {
        var keypad = new Kim1KeypadState();
        keypad.PressKey("5");

        byte rowBits = keypad.GetRowState(0xFE, 0xFF); // column 0 active, DDR = all outputs
        rowBits.Should().Be(0x0F); // no key in this column
    }

    [TestMethod]
    public void Kim1KeypadMatrix_ReleaseKeyClearsInput()
    {
        var keypad = new Kim1KeypadState();
        keypad.PressKey("5");
        keypad.ReleaseKey("5");

        byte rowBits = keypad.GetRowState(0xFD, 0xFF);
        rowBits.Should().Be(0x0F);
    }

    [TestMethod]
    public void Kim1KeypadMatrix_NoColumnsSelected_ReturnsIdle()
    {
        var keypad = new Kim1KeypadState();
        keypad.PressKey("5");

        byte rowBits = keypad.GetRowState(0xFF, 0xFF); // no column active (all high)
        rowBits.Should().Be(0x0F);
    }

    [TestMethod]
    public void Kim1KeypadMatrix_GetKeyMatrix_ReturnsCorrectCoordinates()
    {
        var keypad = new Kim1KeypadState();
        var (row, col) = keypad.GetKeyMatrix("F");
        row.Should().Be(3);
        col.Should().Be(3);
    }

    // --- Phase 3: PA7 edge detect ---

    [TestMethod]
    public void Kim1Riot6530_Pa7FallingEdge_SetsIrqPending()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: true, risingEdge: false);
        riot.SetPortAInput(0x80); // PA7 = 1
        riot.SetPortAInput(0x00); // PA7 = 0 (falling edge)

        riot.Pa7IrqPending.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Riot6530_Pa7RisingEdge_DoesNotSetIrqPending()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: true, risingEdge: false);
        riot.SetPortAInput(0x00); // PA7 = 0
        riot.SetPortAInput(0x80); // PA7 = 1 (rising edge)

        riot.Pa7IrqPending.Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Riot6530_TimerIrq_IndependentFromPa7Irq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: true, risingEdge: false);
        riot.Write(0x1708, 5); // timer with IRQ enabled
        riot.Tick(5);

        riot.TimerIrqPendingRaw.Should().BeTrue();
        riot.Pa7IrqPending.Should().BeFalse();
        riot.IrqPending.Should().BeTrue();

        // Now trigger PA7 IRQ
        riot.SetPortAInput(0x80);
        riot.SetPortAInput(0x00);
        riot.Pa7IrqPending.Should().BeTrue();

        // Both should show on combined line
        riot.IrqPending.Should().BeTrue();
    }

    // --- Phase 4: Timer underflow hardening ---

    [TestMethod]
    public void Kim1Riot6530_IrqDisable_DeactivatesIrqLine()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 5);
        riot.Tick(5);
        riot.IrqPending.Should().BeTrue();

        riot.Write(0x1709, 0); // IRQ disable
        riot.IrqPending.Should().BeFalse();
    }

    [TestMethod]
    public void Kim1Riot6530_IrqDisable_DoesNotClearUnderflowFlag()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1704, 5);
        riot.Tick(5);
        riot.TimerUnderflow.Should().BeTrue();

        riot.Write(0x1709, 0); // IRQ disable
        riot.TimerUnderflow.Should().BeTrue();
    }

    [TestMethod]
    public void Kim1Riot6530_TimerReload_ClearsUnderflowAndIrq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 5); // timer with IRQ
        riot.Tick(5);
        riot.TimerUnderflow.Should().BeTrue();
        riot.TimerIrqPendingRaw.Should().BeTrue();

        riot.Write(0x1704, 10); // reload timer
        riot.TimerUnderflow.Should().BeFalse();
        riot.TimerIrqPendingRaw.Should().BeFalse();
    }

    // --- Phase 4: ClearTimerFlags via read ---

    [TestMethod]
    public void Kim1Riot6530_ReadFlagRegister_ClearsTimerUnderflowOnly()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 5);
        riot.Tick(5);
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: true, risingEdge: false);

        riot.SetPortAInput(0x80);
        riot.SetPortAInput(0x00);
        riot.Pa7IrqPending.Should().BeTrue();

        riot.Read(0x1705); // timer flag read clears timer flags
        riot.TimerUnderflow.Should().BeFalse();
        riot.TimerIrqPendingRaw.Should().BeFalse();

        // PA7 IRQ should remain
        riot.Pa7IrqPending.Should().BeTrue();
    }

    // --- Phase 5: PA7 configurable edge detect ---

    [TestMethod]
    public void Pa7Irq_DisabledByDefault_DoesNotTriggerOnFallingEdge()
    {
        var riot = new Kim1Riot6530IoDevice();
        // PA7 IRQ not enabled by default
        riot.SetPortAInput(0x80);
        riot.SetPortAInput(0x00);

        riot.Pa7IrqPending.Should().BeFalse();
    }

    [TestMethod]
    public void Pa7Irq_WhenConfiguredForFallingEdge_TriggersOnOneToZero()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: true, risingEdge: false);
        riot.SetPortAInput(0x80);
        riot.SetPortAInput(0x00);

        riot.Pa7IrqPending.Should().BeTrue();
    }

    [TestMethod]
    public void Pa7Irq_WhenConfiguredForRisingEdge_TriggersOnZeroToOne()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: false, risingEdge: true);
        riot.SetPortAInput(0x00);
        riot.SetPortAInput(0x80);

        riot.Pa7IrqPending.Should().BeTrue();
    }

    [TestMethod]
    public void Pa7Irq_WhenConfiguredForFallingEdge_DoesNotTriggerOnRisingEdge()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: true, risingEdge: false);
        riot.SetPortAInput(0x00);
        riot.SetPortAInput(0x80);

        riot.Pa7IrqPending.Should().BeFalse();
    }

    [TestMethod]
    public void Pa7Irq_ClearPa7Irq_ClearsOnlyPa7Flag()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: true, risingEdge: false);
        riot.Write(0x1708, 5);
        riot.Tick(5);
        riot.SetPortAInput(0x80);
        riot.SetPortAInput(0x00);

        riot.TimerIrqPendingRaw.Should().BeTrue();
        riot.Pa7IrqPending.Should().BeTrue();

        riot.ClearPa7Irq();
        riot.Pa7IrqPending.Should().BeFalse();
        riot.TimerIrqPendingRaw.Should().BeTrue();
    }

    [TestMethod]
    public void TimerFlagRead_DoesNotClearPa7Irq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 5);
        riot.Tick(5);
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: true, risingEdge: false);
        riot.SetPortAInput(0x80);
        riot.SetPortAInput(0x00);

        riot.Pa7IrqPending.Should().BeTrue();

        riot.Read(0x1705);
        riot.Pa7IrqPending.Should().BeTrue();
    }

    [TestMethod]
    public void ClearIrq_ClearsTimerAndPa7Irq()
    {
        var riot = new Kim1Riot6530IoDevice();
        riot.Write(0x1708, 5);
        riot.Tick(5);
        riot.ConfigurePa7Irq(enabled: true, fallingEdge: true, risingEdge: false);
        riot.SetPortAInput(0x80);
        riot.SetPortAInput(0x00);

        riot.IrqPending.Should().BeTrue();

        riot.ClearIrq();
        riot.IrqPending.Should().BeFalse();
        riot.TimerIrqPendingRaw.Should().BeFalse();
        riot.Pa7IrqPending.Should().BeFalse();
    }

    // --- Phase 5: Keypad matrix enhanced tests ---

    [TestMethod]
    public void KeypadMatrix_NoColumnActive_ReturnsIdleRows()
    {
        var keypad = new Kim1KeypadState();
        keypad.PressKey("5");

        byte rowBits = keypad.GetRowState(0xFF, 0x0F); // all outputs high
        rowBits.Should().Be(0x0F);
    }

    [TestMethod]
    public void KeypadMatrix_SelectedColumnLowAndDdrOutput_ReturnsPressedRowLow()
    {
        var keypad = new Kim1KeypadState();
        keypad.PressKey("5");

        byte rowBits = keypad.GetRowState(0xFD, 0x0F); // col 1 output low, DDR = lower nibble output
        rowBits.Should().Be(0x0D);
    }

    [TestMethod]
    public void KeypadMatrix_SelectedColumnLowButDdrInput_DoesNotActivateColumn()
    {
        var keypad = new Kim1KeypadState();
        keypad.PressKey("5");

        byte rowBits = keypad.GetRowState(0xFD, 0x00); // col 1 low but DDR = all input
        rowBits.Should().Be(0x0F);
    }

    [TestMethod]
    public void KeypadMatrix_MultipleColumnsActive_ReturnsCombinedRows()
    {
        var keypad = new Kim1KeypadState();
        keypad.PressKey("5"); // col 1, row 1
        keypad.PressKey("8"); // col 0, row 2

        byte rowBits = keypad.GetRowState(0xFC, 0x0F); // cols 0 and 1 low, DDR = lower nibble output
        // Row 1 for key 5 and row 2 for key 8
        rowBits.Should().Be(0x09); // bits 1 and 2 low = 0000 1001
    }

    // --- Phase 5: ComputerMachine keypad scan tests ---

    private const string Kim1FullProfile = """
    {
        "id": "kim-1-full",
        "name": "KIM-1 Full",
        "cpu": "mos6502",
        "clockHz": 1000000,
        "memory": {
            "ram": [{ "start": "0x0000", "size": "0x0400" }],
            "rom": [
                { "start": "0x1800", "size": "0x0400" },
                { "start": "0x1C00", "size": "0x0400" }
            ],
            "vectors": { "reset": "0x1C00", "nmi": "0x1C00", "irq": "0x1C00" }
        },
        "devices": [
            { "type": "kim1-6530-io", "id": "riot6530", "start": "0x1700", "size": "0x0100" },
            { "type": "kim1-6530-003-io", "id": "riot6530-003", "start": "0x1400", "size": "0x0100" },
            { "type": "kim1-led-display", "id": "display", "digits": 6 },
            { "type": "kim1-keypad", "id": "keypad" }
        ]
    }
    """;

    [TestMethod]
    public void ComputerMachine_KeypadScan_PropagatesRowsFrom002To003()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Kim1FullProfile);

        machine.Kim1Riot.Should().NotBeNull();
        machine.Kim1Riot003.Should().NotBeNull();
        machine.Kim1Keypad.Should().NotBeNull();

        // Configure column DDR on 002
        machine.Kim1Riot!.Write(0x1701, 0x0F); // Port A DDR = lower nibble output

        // Press key "5" (col 1, row 1)
        machine.Kim1Keypad!.PressKey("5");

        // Set column output: column 1 = low (0xFD), others high
        machine.Kim1Riot.Write(0x1700, 0xFD);

        // Call the keypad matrix update
        machine.Step();

        // 6530-003 Port A should have row 1 (key 5) pulled low
        byte input = machine.Kim1Riot003!.PortAInputValue;
        (input & 0x02).Should().Be(0); // PA1 = row 1 = low (key 5)
    }

    [TestMethod]
    public void ComputerMachine_KeypadScan_DoesNotDestroyNonKeypadInputBits()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Kim1FullProfile);

        machine.Kim1Riot003!.SetPortAInput(0xC0); // upper 2 bits set externally

        machine.Kim1Riot!.Write(0x1701, 0x0F); // Port A DDR = lower nibble output
        machine.Kim1Riot.Write(0x1700, 0xFD);
        machine.Kim1Keypad!.PressKey("5");

        machine.Step();

        byte input = machine.Kim1Riot003.PortAInputValue;
        (input & 0xC0).Should().Be(0xC0); // upper bits preserved
        (input & 0x3F).Should().NotBe(0x3F); // lower bits modified by keypad
    }
}
