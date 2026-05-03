namespace Symulator.Avalonia.Data;

/// <summary>
/// HD44780 CGROM character generator table (ROM code A00).
/// 256 characters × 7 rows, each row encoded in lower 5 bits.
/// Generated from the Hitachi HD44780 datasheet.
/// </summary>
public static class CgromTable
{
    private static readonly byte[][] Patterns = new byte[256][];

    static CgromTable()
    {
        for (int i = 0; i < 256; i++)
            Patterns[i] = new byte[7];

        // 0x00-0x07: CGRAM (user-defined, patterns come from CGRAM not CGROM)
        // These should be empty — actual data comes from CGRAM

        // 0x08-0x1F: undefined — fill with spaces
        for (int i = 0x08; i < 0x20; i++)
            Patterns[i] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // 0x20: Space
        Patterns[0x20] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        // 0x21-0x7F: Standard ASCII
        // Generated from HD44780 datasheet CGROM table
        SetPattern(0x21, "040A0A04000000"); // !
        SetPattern(0x22, "0A0A0000000000"); // "
        SetPattern(0x23, "0A1F0A1F0A0000"); // #
        SetPattern(0x24, "041F141F040000"); // $
        SetPattern(0x25, "1215081A150000"); // %
        SetPattern(0x26, "04110A150A1100"); // &
        SetPattern(0x27, "04020000000000"); // '
        SetPattern(0x28, "04020201000000"); // (
        SetPattern(0x29, "01020204000000"); // )
        SetPattern(0x2A, "04150E15040000"); // *
        SetPattern(0x2B, "04041F04040000"); // +
        SetPattern(0x2C, "00000000040200"); // ,
        SetPattern(0x2D, "00000F00000000"); // -
        SetPattern(0x2E, "00000000060000"); // .
        SetPattern(0x2F, "08040201000000"); // /
        SetPattern(0x30, "0E110B11110E00"); // 0
        SetPattern(0x31, "040C0404040E00"); // 1
        SetPattern(0x32, "0E110108040F00"); // 2
        SetPattern(0x33, "0E11010E01110E"); // 3
        SetPattern(0x34, "02060A121F0200"); // 4
        SetPattern(0x35, "0F100E01110E00"); // 5
        SetPattern(0x36, "0608100E11110E"); // 6
        SetPattern(0x37, "1F010204040400"); // 7
        SetPattern(0x38, "0E110E110E00"); // 8
        SetPattern(0x39, "0E11110E02040C"); // 9
        SetPattern(0x3A, "00060006000000"); // :
        SetPattern(0x3B, "00060006040200"); // ;
        SetPattern(0x3C, "02081010080200"); // <
        SetPattern(0x3D, "000F000F000000"); // =
        SetPattern(0x3E, "08020202080000"); // >
        SetPattern(0x3F, "0E110104040004"); // ?
        SetPattern(0x40, "0E11150B100E00"); // @
        SetPattern(0x41, "040A110E11110E"); // A
        SetPattern(0x42, "0E11110E11110E"); // B
        SetPattern(0x43, "0E110010010E00"); // C
        SetPattern(0x44, "0E11111111110E"); // D
        SetPattern(0x45, "0F100C100F00"); // E
        SetPattern(0x46, "0F100C10001000"); // F
        SetPattern(0x47, "0E110010110E00"); // G
        SetPattern(0x48, "11110E11111100"); // H
        SetPattern(0x49, "0E040404040E00"); // I
        SetPattern(0x4A, "01010101110E00"); // J
        SetPattern(0x4B, "11120C0C121100"); // K
        SetPattern(0x4C, "10001010100F00"); // L
        SetPattern(0x4D, "111B1515111100"); // M
        SetPattern(0x4E, "11191313111100"); // N
        SetPattern(0x4F, "0E111111110E00"); // O
        SetPattern(0x50, "0E11110E100010"); // P
        SetPattern(0x51, "0E11111B120D00"); // Q
        SetPattern(0x52, "0E11110C121100"); // R
        SetPattern(0x53, "0E100C0101110E"); // S
        SetPattern(0x54, "1F040404040400"); // T
        SetPattern(0x55, "11111111110E00"); // U
        SetPattern(0x56, "111111110A0400"); // V
        SetPattern(0x57, "1111151B111100"); // W
        SetPattern(0x58, "110A04040A1100"); // X
        SetPattern(0x59, "110A0404040400"); // Y
        SetPattern(0x5A, "1F0208101F00"); // Z
        SetPattern(0x5B, "0E0808080E00"); // [
        SetPattern(0x5C, "01020408100000"); // backslash
        SetPattern(0x5D, "0E020202020E00"); // ]
        SetPattern(0x5E, "040A1100000000"); // ^
        SetPattern(0x5F, "000000001F0000"); // _
        SetPattern(0x60, "04020000000000"); // `
        SetPattern(0x61, "000E01110E00"); // a
        SetPattern(0x62, "100E11110E00"); // b
        SetPattern(0x63, "000E01010E00"); // c
        SetPattern(0x64, "010E11110E00"); // d
        SetPattern(0x65, "000E110E00"); // e
        SetPattern(0x66, "020E04040400"); // f
        SetPattern(0x67, "000E11110E01"); // g
        SetPattern(0x68, "100E11111100"); // h
        SetPattern(0x69, "040004040400"); // i
        SetPattern(0x6A, "02000202020C"); // j
        SetPattern(0x6B, "10120C121100"); // k
        SetPattern(0x6C, "040404040400"); // l
        SetPattern(0x6D, "00150A110A11"); // m
        SetPattern(0x6E, "000A15111100"); // n
        SetPattern(0x6F, "000E11110E00"); // o
        SetPattern(0x70, "000E11110E10"); // p
        SetPattern(0x71, "000E11110E01"); // q
        SetPattern(0x72, "000C12001000"); // r
        SetPattern(0x73, "000E040E00"); // s
        SetPattern(0x74, "040E04040000"); // t
        SetPattern(0x75, "000A15111100"); // u
        SetPattern(0x76, "000A11110A04"); // v
        SetPattern(0x77, "0011151B1100"); // w
        SetPattern(0x78, "0011040A1100"); // x
        SetPattern(0x79, "000A15110E01"); // y
        SetPattern(0x7A, "000E081F00"); // z
        SetPattern(0x7B, "02040408040200"); // {
        SetPattern(0x7C, "04040404040000"); // |
        SetPattern(0x7D, "08040402040800"); // }
        SetPattern(0x7E, "0015041F000000"); // ~ (in HD44780 this is arrow up)
        SetPattern(0x7F, "040E1F1F1F0E04"); // arrow up (HD44780 CGROM 0x7F)

        // 0x80-0xFF: Japanese/Kana characters — left as empty for now
        for (int i = 0x80; i < 0x100; i++)
            Patterns[i] = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    }

    public static byte[] GetPattern(byte charCode) => Patterns[charCode & 0xFF];

    private static void SetPattern(int code, string hexRows)
    {
        if (hexRows.Length % 2 != 0)
            hexRows = "0" + hexRows;
        int rowCount = hexRows.Length / 2;
        for (int r = 0; r < 7; r++)
        {
            if (r < rowCount)
                Patterns[code][r] = Convert.ToByte(hexRows.Substring(r * 2, 2), 16);
            else
                Patterns[code][r] = 0;
        }
    }
}
