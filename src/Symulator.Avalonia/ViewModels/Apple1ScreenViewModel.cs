using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Symulator.Machines.Apple1.Devices;

namespace Symulator.Avalonia.ViewModels;

public sealed class Apple1ScreenViewModel : INotifyPropertyChanged
{
    private readonly Apple1TerminalBuffer _buffer;
    private int _rows = 24;
    private int _columns = 40;
    private double _scale = 1.0;
    private Color _foreground = Color.Parse("#4DFF88");
    private Color _background = Color.Parse("#1a1a1a");

    public Apple1ScreenViewModel(Apple1TerminalBuffer buffer)
    {
        _buffer = buffer;
    }

    public int Rows { get => _rows; set { _rows = value; OnPropertyChanged(); } }
    public int Columns { get => _columns; set { _columns = value; OnPropertyChanged(); } }
    public double Scale { get => _scale; set { _scale = value; OnPropertyChanged(); } }
    public Color ForegroundColor { get => _foreground; set { _foreground = value; OnPropertyChanged(); } }
    public Color BackgroundColor { get => _background; set { _background = value; OnPropertyChanged(); } }
    public char[,] Cells => _buffer.Cells;
    public int CursorRow => _buffer.CursorRow;
    public int CursorColumn => _buffer.CursorColumn;
    public long BufferVersion => _buffer.Version;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void Invalidate()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Cells)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CursorRow)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CursorColumn)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BufferVersion)));
    }
}
