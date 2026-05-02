namespace CmosCpu.BlazorApp.Models;

public class MemoryRowViewModel
{
    public ushort Address { get; set; }
    public string AddressText => $"0x{Address:X4}";

    public string[] Cells { get; } = new string[16];
    public bool[] IsPc { get; } = new bool[16];
    public string AsciiText { get; private set; } = "";

    private readonly char[] _asciiChars = new char[16];

    public void SetCell(int index, byte value, bool isPC)
    {
        if (index < 0 || index >= 16) return;
        Cells[index] = $"0x{value:X2}";
        IsPc[index] = isPC;
        _asciiChars[index] = value >= 0x20 && value <= 0x7E ? (char)value : '.';
        AsciiText = new string(_asciiChars);
    }

    public string RowClass
    {
        get
        {
            for (int i = 0; i < 16; i++)
                if (IsPc[i]) return "memory-row-pc";
            return "";
        }
    }
}