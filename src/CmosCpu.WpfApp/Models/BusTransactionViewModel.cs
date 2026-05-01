using CmosCpu.Core;

namespace CmosCpu.WpfApp.Models;

public class BusTransactionViewModel : ViewModels.ViewModelBase
{
    private string _cycle = "";
    private string _operation = "";
    private string _address = "";
    private string _value = "";
    private string _device = "";

    public string Cycle { get => _cycle; set => SetProperty(ref _cycle, value); }
    public string Operation { get => _operation; set => SetProperty(ref _operation, value); }
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    public string Value { get => _value; set => SetProperty(ref _value, value); }
    public string Device { get => _device; set => SetProperty(ref _device, value); }

    public static BusTransactionViewModel FromTransaction(BusTransaction tx)
    {
        return new BusTransactionViewModel
        {
            Cycle = tx.Cycle.ToString(),
            Operation = tx.Operation == BusOperation.Read ? "READ" : "WRITE",
            Address = $"0x{tx.Address:X4}",
            Value = $"0x{tx.Value:X2}",
            Device = tx.DeviceName,
        };
    }
}
