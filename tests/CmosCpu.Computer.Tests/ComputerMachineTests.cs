using CmosCpu.Computer;
using CmosCpu.Core;
using FluentAssertions;

namespace CmosCpu.Computer.Tests;

[TestClass]
public sealed class ComputerMachineTests
{
    internal const string Retro70Profile = """
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

    private const string MinimalProfile = """
    {
        "id": "minimal",
        "name": "Minimal",
        "cpu": "mos6502",
        "clockHz": 1000000,
        "memory": {
            "ram": [{ "start": "0x0000", "size": "0x8000" }]
        }
    }
    """;

    [TestMethod]
    public void MemoryBus_RamReadWrite_Works()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRam(0x0000, 0x8000);

        bus.WriteByte(0x1000, 0xAB);
        bus.ReadByte(0x1000).Should().Be(0xAB);
    }

    [TestMethod]
    public void MemoryBus_RomReadOnly_IgnoresWrites()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRom(0xF000, 0x1000);
        bus.LoadRom(0xF000, [0xAA, 0xBB, 0xCC]);

        bus.WriteByte(0xF000, 0xFF);
        bus.ReadByte(0xF000).Should().Be(0xAA);
    }

    [TestMethod]
    public void MemoryBus_UnmappedAddress_ReturnsOpenBusValue()
    {
        var bus = new ComputerMemoryBus();
        bus.MapRam(0x0000, 0x1000);

        bus.ReadByte(0xFFFF).Should().Be(0xFF);
    }

    [TestMethod]
    public void MemoryBus_DeviceMapping_Works()
    {
        var bus = new ComputerMemoryBus();
        byte lastWritten = 0;
        bus.MapDevice(0xD000, 3,
            addr => (byte)(addr == 0xD000 ? 0x42 : 0),
            (addr, val) => { if (addr == 0xD001) lastWritten = val; });

        bus.WriteByte(0xD001, 0x77);
        lastWritten.Should().Be(0x77);
        bus.ReadByte(0xD000).Should().Be(0x42);
    }

    [TestMethod]
    public void ComputerMachine_CreateFromProfile_HasCorrectProperties()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Retro70Profile);

        machine.Profile.Id.Should().Be("retro70-mos6502");
        machine.Profile.Name.Should().Be("Retro70 MOS 6502 Computer");
        machine.Cpu.Should().NotBeNull();
        machine.Memory.Should().NotBeNull();
        machine.TextDisplay.Should().NotBeNull();
        machine.Keyboard.Should().NotBeNull();
    }

    [TestMethod]
    public void ComputerMachine_Reset_SetsPcFromVector()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Retro70Profile);
        var rom = new byte[0x1000];
        rom[0xFFC] = 0x00;
        rom[0xFFD] = 0xF0;
        machine.Memory.LoadRom(0xF000, rom);

        machine.Reset();

        machine.Cpu.PC.Should().Be(0xF000);
    }

    [TestMethod]
    public void ComputerMachine_Reset_SetsSp()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Retro70Profile);
        machine.Reset();

        machine.Cpu.SP.Should().Be(0xFD);
    }

    [TestMethod]
    public void ComputerMachine_Step_ExecutesInstruction()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Retro70Profile);
        // Load NOPs and reset vector into ROM
        var rom = new byte[0x1000];
        rom[0x000] = 0xEA; // NOP
        rom[0x001] = 0xEA; // NOP
        // Reset vector at 0xFFFC -> offset 0xFFC in ROM (ROM starts at 0xF000)
        rom[0xFFC] = 0x00; // low byte
        rom[0xFFD] = 0xF0; // high byte -> 0xF000
        machine.Memory.LoadRom(0xF000, rom);

        machine.Reset();
        ulong cyclesBefore = machine.Cpu.CycleCount;

        machine.Step();

        machine.Cpu.CycleCount.Should().BeGreaterThan(cyclesBefore);
    }

    [TestMethod]
    public void ComputerMachine_RunSteps_ExecutesMultipleInstructions()
    {
        var machine = ComputerMachineFactory.CreateFromProfile(Retro70Profile);
        var rom = new byte[0x1000];
        rom[0x000] = 0xEA;
        rom[0x001] = 0xEA;
        rom[0x002] = 0xEA;
        rom[0x003] = 0xEA;
        rom[0xFFC] = 0x00;
        rom[0xFFD] = 0xF0;
        machine.Memory.LoadRom(0xF000, rom);

        machine.Reset();
        ulong cyclesBefore = machine.Cpu.CycleCount;

        machine.RunSteps(4);

        machine.Cpu.CycleCount.Should().BeGreaterThan(cyclesBefore);
    }

    [TestMethod]
    public void TextDisplayRegion_WritesAndReads()
    {
        var display = new TextDisplayRegion(0xD000, 40, 25);

        display.Write(0xD000, (byte)'H');
        display.Write(0xD001, (byte)'i');

        display.Read(0xD000).Should().Be((byte)'H');
        display.Read(0xD001).Should().Be((byte)'i');
    }

    [TestMethod]
    public void TextDisplayRegion_GetChar_ReturnsCharacter()
    {
        var display = new TextDisplayRegion(0xD000, 40, 25);

        display.Write(0xD000, (byte)'A');
        display.Write(0xD028, (byte)'B'); // row 1, col 0 (0xD000 + 40 = 0xD028)

        display.GetChar(0, 0).Should().Be('A');
        display.GetChar(0, 1).Should().Be('B');
    }

    [TestMethod]
    public void TextDisplayRegion_NonAscii_ReturnsDot()
    {
        var display = new TextDisplayRegion(0xD000, 40, 25);

        display.Write(0xD000, 0x01);
        display.Write(0xD001, 0xFF);

        display.GetChar(0, 0).Should().Be('.');
        display.GetChar(1, 0).Should().Be('.');
    }

    [TestMethod]
    public void TextDisplayRegion_Clear_ResetsAll()
    {
        var display = new TextDisplayRegion(0xD000, 40, 25);
        display.Write(0xD000, (byte)'X');

        display.Clear();

        display.Read(0xD000).Should().Be(0);
    }

    [TestMethod]
    public void KeyboardRegion_EnqueueAndRead()
    {
        var keyboard = new KeyboardRegion(0xD800, 0xD801);

        keyboard.EnqueueKey('A');
        keyboard.EnqueueKey('B');

        keyboard.ReadStatus().Should().Be(1);
        keyboard.ReadData().Should().Be((byte)'A');
        keyboard.ReadData().Should().Be((byte)'B');
        keyboard.ReadStatus().Should().Be(0);
    }

    [TestMethod]
    public void KeyboardRegion_EmptyBuffer_ReturnsZeroStatus()
    {
        var keyboard = new KeyboardRegion(0xD800, 0xD801);

        keyboard.ReadStatus().Should().Be(0);
    }

    [TestMethod]
    public void ComputerMachine_Factory_CreatesFromFile()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, Retro70Profile);

            var machine = ComputerMachineFactory.CreateFromFile(path);

            machine.Should().NotBeNull();
            machine.Profile.Id.Should().Be("retro70-mos6502");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ComputerMachine_Factory_InvalidProfile_Throws()
    {
        Action act = () => ComputerMachineFactory.CreateFromProfile("{ invalid }");
        act.Should().Throw<ArgumentException>();
    }
}
