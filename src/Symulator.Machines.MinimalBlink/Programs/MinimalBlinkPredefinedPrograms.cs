using Symulator.Machines.MinimalBlink.Models;

namespace Symulator.Machines.MinimalBlink.Programs;

public static class MinimalBlinkPredefinedPrograms
{
    private static readonly List<MinimalBlinkPredefinedProgram> Programs = new()
    {
        new MinimalBlinkPredefinedProgram(
            Id: "blink-led",
            Name: "Blink LED",
            Description: "Toggles LED port forever using a small software delay loop.",
            LoadAddress: 0x0100,
            StartAddress: 0x0100,
            Bytes: new byte[]
            {
                0x01, 0x01,
                0x02, 0x00, 0xFF,
                0x01, 0x20,
                0x09, 0x00,
                0x05, 0x00, 0x00,
                0x06, 0x09, 0x01,
                0x01, 0x00,
                0x02, 0x00, 0xFF,
                0x01, 0x20,
                0x09, 0x00,
                0x05, 0x00, 0x00,
                0x06, 0x18, 0x01,
                0x03, 0x00, 0x01,
            }),

        new MinimalBlinkPredefinedProgram(
            Id: "led-on",
            Name: "LED ON",
            Description: "Turns LED on and halts.",
            LoadAddress: 0x0100,
            StartAddress: 0x0100,
            Bytes: new byte[] { 0x01, 0x01, 0x02, 0x00, 0xFF, 0x07 }),

        new MinimalBlinkPredefinedProgram(
            Id: "hello-lcd",
            Name: "Hello World (LCD)",
            Description: "Initializes 16x2 LCD and displays Hello World! then halts.",
            LoadAddress: 0x0100,
            StartAddress: 0x0100,
            Bytes: GetHelloWorldBytes()),

        new MinimalBlinkPredefinedProgram(
            Id: "hello-lcd-poll",
            Name: "Hello World (LCD, busy-poll)",
            Description: "LCD Hello World with busy-flag polling.",
            LoadAddress: 0x0100,
            StartAddress: 0x0100,
            Bytes: GetHelloWorldPollBytes()),

        new MinimalBlinkPredefinedProgram(
            Id: "hello-lcd-i2c",
            Name: "Hello LCD via PCF8574 (I2C)",
            Description: "Initializes LCD through I2C+PCF8574 backpack, writes 'A'. Uses $FE30-$FE33.",
            LoadAddress: 0x0100,
            StartAddress: 0x0100,
            Bytes: GetI2cLcdBytes()),

        new MinimalBlinkPredefinedProgram(
            Id: "hello-uart",
            Name: "Hello UART Terminal",
            Description: "Writes 'Hello UART' to the UART terminal via $FE40.",
            LoadAddress: 0x0100,
            StartAddress: 0x0100,
            Bytes: GetHelloUartBytes()),

        new MinimalBlinkPredefinedProgram(
            Id: "i2c-scan",
            Name: "I2C Scan → UART",
            Description: "Scans I2C bus 0x01-0x7F, outputs found addresses via UART.",
            LoadAddress: 0x0100,
            StartAddress: 0x0100,
            Bytes: GetI2cScanBytes()),
    };

    private static byte[] GetHelloWorldBytes()
    {
        return new byte[]
        {
            0x01, 0x38, 0x02, 0x00, 0xFE,
            0x01, 0x0C, 0x02, 0x00, 0xFE,
            0x01, 0x06, 0x02, 0x00, 0xFE,
            0x01, 0x01, 0x02, 0x00, 0xFE,
            0x01, 0x48, 0x02, 0x01, 0xFE,
            0x01, 0x65, 0x02, 0x01, 0xFE,
            0x01, 0x6C, 0x02, 0x01, 0xFE,
            0x01, 0x6C, 0x02, 0x01, 0xFE,
            0x01, 0x6F, 0x02, 0x01, 0xFE,
            0x01, 0x20, 0x02, 0x01, 0xFE,
            0x01, 0x57, 0x02, 0x01, 0xFE,
            0x01, 0x6F, 0x02, 0x01, 0xFE,
            0x01, 0x72, 0x02, 0x01, 0xFE,
            0x01, 0x6C, 0x02, 0x01, 0xFE,
            0x01, 0x64, 0x02, 0x01, 0xFE,
            0x01, 0x21, 0x02, 0x01, 0xFE,
            0x07,
        };
    }

    private static byte[] GetHelloWorldPollBytes()
    {
        return new byte[]
        {
            0x0D, 0x81, 0x01, 0x01, 0x38, 0x02, 0x00, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x0C, 0x02, 0x00, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x06, 0x02, 0x00, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x01, 0x02, 0x00, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x48, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x65, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x6C, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x6C, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x6F, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x20, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x57, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x6F, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x72, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x6C, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x64, 0x02, 0x01, 0xFE,
            0x0D, 0x81, 0x01, 0x01, 0x21, 0x02, 0x01, 0xFE,
            0x07,
            0x08, 0x00, 0xFE, 0x0B, 0x80, 0x0C, 0xF9, 0x0E,
        };
    }

    private static byte[] GetI2cLcdBytes()
    {
        var bytes = new List<byte>();

        void Emit(params byte[] b) => bytes.AddRange(b);

        void LdaStaAbs(byte val, ushort addr)
        {
            Emit(0x01, val, 0x02, (byte)(addr & 0xFF), (byte)(addr >> 8));
        }

        void I2cWrite(byte val)
        {
            LdaStaAbs(val, 0xFE32);
            LdaStaAbs(0x04, 0xFE30);
        }

        void SendNibblePair(byte rs, byte nibble)
        {
            byte data = (byte)(nibble << 4);
            byte bl = 0x08;
            I2cWrite((byte)(data | bl | rs | 0x04));
            I2cWrite((byte)(data | bl | rs));
        }

        void SendLcdCmd(byte cmd)
        {
            SendNibblePair(0, (byte)(cmd >> 4));
            SendNibblePair(0, (byte)(cmd & 0x0F));
        }

        void SendLcdData(byte ch)
        {
            SendNibblePair(1, (byte)(ch >> 4));
            SendNibblePair(1, (byte)(ch & 0x0F));
        }

        void InlineDelay()
        {
            Emit(0x01, 0x60);
            Emit(0x09, 0x80);
            int decAddr = 0x0100 + bytes.Count;
            Emit(0x05, 0x80, 0x00);
            Emit(0x06, (byte)(decAddr & 0xFF), (byte)((decAddr >> 8) & 0xFF));
        }

        LdaStaAbs(0x27, 0xFE31);

        SendLcdCmd(0x28); InlineDelay();
        SendLcdCmd(0x0C); InlineDelay();
        SendLcdCmd(0x06); InlineDelay();
        SendLcdCmd(0x01); InlineDelay();

        SendLcdData((byte)'A');

        Emit(0x07);

        return bytes.ToArray();
    }

    private static byte[] GetHelloUartBytes()
    {
        return
        [
            0x01, (byte)'H', 0x02, 0x40, 0xFE,
            0x01, (byte)'e', 0x02, 0x40, 0xFE,
            0x01, (byte)'l', 0x02, 0x40, 0xFE,
            0x01, (byte)'l', 0x02, 0x40, 0xFE,
            0x01, (byte)'o', 0x02, 0x40, 0xFE,
            0x01, (byte)' ', 0x02, 0x40, 0xFE,
            0x01, (byte)'U', 0x02, 0x40, 0xFE,
            0x01, (byte)'A', 0x02, 0x40, 0xFE,
            0x01, (byte)'R', 0x02, 0x40, 0xFE,
            0x01, (byte)'T', 0x02, 0x40, 0xFE,
            0x01, 0x0D,      0x02, 0x40, 0xFE,
            0x01, 0x0A,      0x02, 0x40, 0xFE,
            0x07,
        ];
    }

    private static byte[] GetI2cScanBytes()
    {
        return
        [
            // "I2C Scan\r" via UART
            0x01, (byte)'I', 0x02, 0x40, 0xFE,
            0x01, (byte)'2', 0x02, 0x40, 0xFE,
            0x01, (byte)'C', 0x02, 0x40, 0xFE,
            0x01, (byte)' ', 0x02, 0x40, 0xFE,
            0x01, (byte)'S', 0x02, 0x40, 0xFE,
            0x01, (byte)'c', 0x02, 0x40, 0xFE,
            0x01, (byte)'a', 0x02, 0x40, 0xFE,
            0x01, (byte)'n', 0x02, 0x40, 0xFE,
            0x01, 0x0D,      0x02, 0x40, 0xFE,

            // Init counter = 127
            0x01, 0x7F, 0x09, 0xF0,              // LDA #$7F, STA_ZP $F0

            // LOOP (at $0133):
            // LDA_ZP $F0 → STA I2C_ADDR ($FE31)
            0x0A, 0xF0, 0x02, 0x31, 0xFE,
            // LDA #0 → STA I2C_DATA ($FE32)
            0x01, 0x00, 0x02, 0x32, 0xFE,
            // LDA #4 → STA I2C_CTRL ($FE30)
            0x01, 0x04, 0x02, 0x30, 0xFE,

            // LDA $FE33 → AND #$01 → BNE skip ($014F)
            0x08, 0x33, 0xFE,                    // LDA $FE33
            0x0B, 0x01,                          // AND #$01
            0x0C, 0x0A,                          // BNE +10 → skip to $0153 (DEC_MEM)

            // FOUND: output byte + space
            0x0A, 0xF0, 0x02, 0x40, 0xFE,        // LDA_ZP $F0, STA $FE40
            0x01, 0x20, 0x02, 0x40, 0xFE,        // LDA #' ', STA $FE40

            // SKIP ($014F):
            // DEC_MEM $00F0, JNZ $0133 (loop)
            0x05, 0xF0, 0x00,                    // DEC_MEM $00F0
            0x06, 0x33, 0x01,                    // JNZ $0133

            // "\rDone\r"
            0x01, 0x0D, 0x02, 0x40, 0xFE,
            0x01, (byte)'D', 0x02, 0x40, 0xFE,
            0x01, (byte)'o', 0x02, 0x40, 0xFE,
            0x01, (byte)'n', 0x02, 0x40, 0xFE,
            0x01, (byte)'e', 0x02, 0x40, 0xFE,
            0x01, 0x0D, 0x02, 0x40, 0xFE,

            // HLT
            0x07,
        ];
    }

    public static IReadOnlyList<MinimalBlinkPredefinedProgram> All => Programs;

    public static MinimalBlinkPredefinedProgram? FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return Programs.FirstOrDefault(p =>
            string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static MinimalBlinkPredefinedProgram? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        return Programs.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
