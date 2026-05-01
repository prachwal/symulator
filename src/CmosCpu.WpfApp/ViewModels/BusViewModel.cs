using System.Collections.ObjectModel;
using CmosCpu.Core;
using CmosCpu.WpfApp.Models;

namespace CmosCpu.WpfApp.ViewModels;

public class BusViewModel : ViewModelBase
{
    private const int MaxEntries = 5000;
    private int _entryCount;

    public ObservableCollection<BusTransactionViewModel> Entries { get; } = new();

    public void AddTransaction(BusTransaction tx)
    {
        var vm = BusTransactionViewModel.FromTransaction(tx);
        Entries.Add(vm);
        _entryCount++;
        if (Entries.Count > MaxEntries)
            Entries.RemoveAt(0);
    }

    public void Clear()
    {
        Entries.Clear();
        _entryCount = 0;
    }
}
