using System.Collections.ObjectModel;
using CmosCpu.Core;
using CmosCpu.WpfApp.Models;

namespace CmosCpu.WpfApp.ViewModels;

public class TraceLogViewModel : ViewModelBase
{
    private const int MaxEntries = 10000;

    public ObservableCollection<TraceEntryViewModel> Entries { get; } = new();

    public void AddEntry(TraceEntry entry)
    {
        Entries.Add(TraceEntryViewModel.FromEntry(entry));
        if (Entries.Count > MaxEntries)
            Entries.RemoveAt(0);
    }

    public void Clear()
    {
        Entries.Clear();
    }
}
