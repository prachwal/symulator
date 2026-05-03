using Symulator.Machines.MinimalBlink.Models;

namespace Symulator.Machines.MinimalBlink.Programs;

public static class MinimalBlinkPredefinedPrograms
{
    private static readonly MinimalBlinkPredefinedProgram[] Programs =
    [
        new MinimalBlinkPredefinedProgram(
            Id: "blink-led",
            Name: "Blink LED",
            Description: "Toggles LED port forever using a small software delay loop.",
            LoadAddress: 0x0100,
            StartAddress: 0x0100,
            Bytes:
            [
                0x01, 0x01,          // LDA_IMM #$01
                0x02, 0x00, 0xFF,    // STA_ABS $FF00   LED ON
                0x01, 0x20,          // LDA_IMM #$20
                0x09, 0x00,          // STA_ZP $00
                0x05, 0x00, 0x00,    // DEC_MEM $0000
                0x06, 0x09, 0x01,    // JNZ_ABS $0109
                0x01, 0x00,          // LDA_IMM #$00
                0x02, 0x00, 0xFF,    // STA_ABS $FF00   LED OFF
                0x01, 0x20,          // LDA_IMM #$20
                0x09, 0x00,          // STA_ZP $00
                0x05, 0x00, 0x00,    // DEC_MEM $0000
                0x06, 0x18, 0x01,    // JNZ_ABS $0118
                0x03, 0x00, 0x01,    // JMP_ABS $0100
            ]),

        new MinimalBlinkPredefinedProgram(
            Id: "led-on",
            Name: "LED ON",
            Description: "Turns LED on and halts.",
            LoadAddress: 0x0100,
            StartAddress: 0x0100,
            Bytes: [0x01, 0x01, 0x02, 0x00, 0xFF, 0x07]),
    ];

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
