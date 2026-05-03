using Symulator.Machines.Kim1.Models;

namespace Symulator.Machines.Kim1.Services;

public static class Kim1PredefinedPrograms
{
    private static readonly Kim1PredefinedProgram[] Programs =
    [
        new Kim1PredefinedProgram(
            Id: "ram-write-loop",
            Name: "RAM write loop",
            Description: "Writes $42 to zero-page RAM address $0000 and loops forever.",
            LoadAddress: 0x0200,
            StartAddress: 0x0200,
            Bytes: [0xA9, 0x42, 0x85, 0x00, 0x4C, 0x04, 0x02],
            ExpectedResultDescription: "After several CPU steps RAM[$0000] should contain $42 and PC should loop at $0204."),

        new Kim1PredefinedProgram(
            Id: "riot-io-test",
            Name: "RIOT I/O ports test",
            Description: "Sets RIOT-002 DDRA/DDRB as outputs ($FF) and writes $AA to Port A, $55 to Port B.",
            LoadAddress: 0x0200,
            StartAddress: 0x0200,
            Bytes:
            [
                0xA9, 0xFF,          // LDA #$FF
                0x8D, 0x01, 0x17,    // STA $1701   DDRA = FF
                0x8D, 0x03, 0x17,    // STA $1703   DDRB = FF
                0xA9, 0xAA,          // LDA #$AA
                0x8D, 0x00, 0x17,    // STA $1700   Port A = AA
                0xA9, 0x55,          // LDA #$55
                0x8D, 0x02, 0x17,    // STA $1702   Port B = 55
                0x4C, 0x12, 0x02,    // JMP $0212   loop
            ],
            ExpectedResultDescription: "After stepping through, RIOT-002 Port A=$AA, Port B=$55, DDRA=$FF, DDRB=$FF."),

        new Kim1PredefinedProgram(
            Id: "display-test",
            Name: "6-digit display test 123456",
            Description: "Shows '123456' on the 6-digit 7-segment LED display.",
            LoadAddress: 0x0200,
            StartAddress: 0x0200,
            Bytes:
            [
                // Set DDRs
                0xA9, 0xFF, 0x8D, 0x01, 0x17,    // DDRA = FF
                0x8D, 0x03, 0x17,                 // DDRB = FF
                // Digit 0: Port A=$00, then Port B='1'=$06
                0xA9, 0x00, 0x8D, 0x00, 0x17,
                0xA9, 0x06, 0x8D, 0x02, 0x17,
                // Digit 1: Port A=$01, then Port B='2'=$5B
                0xA9, 0x01, 0x8D, 0x00, 0x17,
                0xA9, 0x5B, 0x8D, 0x02, 0x17,
                // Digit 2: Port A=$02, then Port B='3'=$4F
                0xA9, 0x02, 0x8D, 0x00, 0x17,
                0xA9, 0x4F, 0x8D, 0x02, 0x17,
                // Digit 3: Port A=$03, then Port B='4'=$66
                0xA9, 0x03, 0x8D, 0x00, 0x17,
                0xA9, 0x66, 0x8D, 0x02, 0x17,
                // Digit 4: Port A=$04, then Port B='5'=$6D
                0xA9, 0x04, 0x8D, 0x00, 0x17,
                0xA9, 0x6D, 0x8D, 0x02, 0x17,
                // Digit 5: Port A=$05, then Port B='6'=$7D
                0xA9, 0x05, 0x8D, 0x00, 0x17,
                0xA9, 0x7D, 0x8D, 0x02, 0x17,
                // Loop
                0x4C, 0x44, 0x02,
            ],
            ExpectedResultDescription: "After stepping through, LED display shows '123456'."),
    ];

    public static IReadOnlyList<Kim1PredefinedProgram> All => Programs;

    public static Kim1PredefinedProgram? FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return Programs.FirstOrDefault(p =>
            string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
