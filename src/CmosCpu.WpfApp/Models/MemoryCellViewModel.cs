using CmosCpu.Core;

namespace CmosCpu.WpfApp.Models;

public class MemoryCellViewModel : ViewModels.ViewModelBase
{
    private ushort _address;
    private byte _value;
    private bool _isPC;
    private bool _isHighlighted;
    private string _hexValue = "00";
    private char _asciiChar = '.';

    public ushort Address { get => _address; set => SetProperty(ref _address, value); }
    public byte Value { get => _value; set { SetProperty(ref _value, value); HexValue = $"0x{value:X2}"; AsciiChar = value >= 0x20 && value <= 0x7E ? (char)value : '.'; } }
    public bool IsPC { get => _isPC; set => SetProperty(ref _isPC, value); }
    public bool IsHighlighted { get => _isHighlighted; set => SetProperty(ref _isHighlighted, value); }
    public string HexValue { get => _hexValue; private set => SetProperty(ref _hexValue, value); }
    public char AsciiChar { get => _asciiChar; private set => SetProperty(ref _asciiChar, value); }
}
