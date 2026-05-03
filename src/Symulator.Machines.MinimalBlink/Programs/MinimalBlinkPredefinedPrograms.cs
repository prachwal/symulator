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
