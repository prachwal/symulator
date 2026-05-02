using System.Text;

namespace CmosCpu.Computer;

public static class Retro70MonitorRomBuilder
{
    public static byte[] Build(int size = 0x1000)
    {
        var rom = new byte[size];

        // Program at 0xF000: print "RETRO70 READY\n>" then infinite loop
        // 0xF000: LDX #0
        rom[0x000] = 0xA2;
        rom[0x001] = 0x00;

        // 0xF002: LDA $F011,X (message at 0xF011)
        rom[0x002] = 0xBD;
        rom[0x003] = 0x11;
        rom[0x004] = 0xF0;

        // 0xF005: BEQ +7 (to 0xF00E if A==0)
        rom[0x005] = 0xF0;
        rom[0x006] = 0x07;

        // 0xF007: STA $D000,X
        rom[0x007] = 0x9D;
        rom[0x008] = 0x00;
        rom[0x009] = 0xD0;

        // 0xF00A: INX
        rom[0x00A] = 0xE8;

        // 0xF00B: JMP $F002
        rom[0x00B] = 0x4C;
        rom[0x00C] = 0x02;
        rom[0x00D] = 0xF0;

        // 0xF00E: JMP $F00E (infinite loop)
        rom[0x00E] = 0x4C;
        rom[0x00F] = 0x0E;
        rom[0x010] = 0xF0;

        // Message at 0xF011: "RETRO70 READY", CR, ">", NUL
        string message = "RETRO70 READY\r>\0";
        byte[] msgBytes = Encoding.ASCII.GetBytes(message);
        Array.Copy(msgBytes, 0, rom, 0x011, msgBytes.Length);

        // Vectors at 0xFFA-0xFFF in CPU space = offset 0xFFA in ROM
        rom[0xFFA] = 0x00; rom[0xFFB] = 0xF1; // NMI -> 0xF100
        rom[0xFFC] = 0x00; rom[0xFFD] = 0xF0; // RESET -> 0xF000
        rom[0xFFE] = 0x00; rom[0xFFF] = 0xF2; // IRQ -> 0xF200

        return rom;
    }
}
