using System.Collections.ObjectModel;
using System.Windows.Input;
using CmosCpu.Core;
using CmosCpu.WpfApp.Commands;

namespace CmosCpu.WpfApp.ViewModels;

public class MemoryViewModel : ViewModelBase
{
    private ushort _startAddress;
    private string _addressInput = "0x8000";

    public ObservableCollection<MemoryLineViewModel> Lines { get; } = new();
    public string AddressInput { get => _addressInput; set => SetProperty(ref _addressInput, value); }
    public ushort StartAddress { get => _startAddress; set => SetProperty(ref _startAddress, value); }

    public ICommand GoToPCCommand { get; }
    public ICommand RefreshCommand { get; }

    public MemoryViewModel()
    {
        GoToPCCommand = new RelayCommand(_ => { }, _ => false);
        RefreshCommand = new RelayCommand(_ => { OnPropertyChanged(nameof(RefreshCommand)); });
    }

    public void Update(SimulatorSnapshot snap)
    {
        Lines.Clear();

        var memDict = new Dictionary<ushort, byte>();
        foreach (var cell in snap.MemoryWindow)
            memDict[cell.Address] = cell.Value;

        ushort addr = StartAddress;
        for (int row = 0; row < 16; row++)
        {
            var line = new MemoryLineViewModel { Address = addr };
            for (int col = 0; col < 16; col++)
            {
                ushort currentAddr = (ushort)(addr + col);
                byte value = memDict.TryGetValue(currentAddr, out var v) ? v : (byte)0;
                line.SetCell(col, value, currentAddr == snap.Registers.PC);
            }
            Lines.Add(line);
            addr += 16;
        }
    }

    public void GoToAddress(ushort address)
    {
        StartAddress = (ushort)(address & 0xFFF0);
        AddressInput = $"0x{StartAddress:X4}";
    }

    public void GoToPC(ushort pc)
    {
        GoToAddress(pc);
    }
}

public class MemoryLineViewModel : ViewModelBase
{
    public ushort Address { get; set; }

    public string AddressText => $"0x{Address:X4}";

    public string Cell0Hex { get; private set; } = "00";
    public string Cell1Hex { get; private set; } = "00";
    public string Cell2Hex { get; private set; } = "00";
    public string Cell3Hex { get; private set; } = "00";
    public string Cell4Hex { get; private set; } = "00";
    public string Cell5Hex { get; private set; } = "00";
    public string Cell6Hex { get; private set; } = "00";
    public string Cell7Hex { get; private set; } = "00";
    public string Cell8Hex { get; private set; } = "00";
    public string Cell9Hex { get; private set; } = "00";
    public string CellAHex { get; private set; } = "00";
    public string CellBHex { get; private set; } = "00";
    public string CellCHex { get; private set; } = "00";
    public string CellDHex { get; private set; } = "00";
    public string CellEHex { get; private set; } = "00";
    public string CellFHex { get; private set; } = "00";
    public string AsciiText { get; private set; } = "";
    public string RowBackground { get; private set; } = "Transparent";

    private readonly string[] _cellProps =
    {
        nameof(Cell0Hex), nameof(Cell1Hex), nameof(Cell2Hex), nameof(Cell3Hex),
        nameof(Cell4Hex), nameof(Cell5Hex), nameof(Cell6Hex), nameof(Cell7Hex),
        nameof(Cell8Hex), nameof(Cell9Hex), nameof(CellAHex), nameof(CellBHex),
        nameof(CellCHex), nameof(CellDHex), nameof(CellEHex), nameof(CellFHex),
    };

    public void SetCell(int index, byte value, bool isPC)
    {
        if (index < 0 || index >= 16) return;
        string hex = $"0x{value:X2}";

        var prop = _cellProps[index];
        var field = GetType().GetProperty(prop);
        field?.SetValue(this, hex);

        if (isPC)
            RowBackground = "#264F78";

        BuildAscii(index, value);
    }

    private char[] _asciiChars = new char[16];

    private void BuildAscii(int index, byte value)
    {
        _asciiChars[index] = value >= 0x20 && value <= 0x7E ? (char)value : '.';
        AsciiText = new string(_asciiChars);
        OnPropertyChanged(nameof(AsciiText));
    }
}
