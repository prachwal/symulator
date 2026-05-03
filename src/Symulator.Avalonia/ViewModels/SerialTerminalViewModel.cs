using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Symulator.Application.Terminal;

namespace Symulator.Avalonia.ViewModels;

public sealed class SerialTerminalViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> Lines { get; } = new();

    public void ApplySnapshot(TerminalSnapshot snapshot)
    {
        Lines.Clear();
        foreach (var line in snapshot.Lines)
            Lines.Add(line);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
