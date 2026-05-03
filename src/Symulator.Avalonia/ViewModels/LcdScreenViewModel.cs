using System.ComponentModel;
using System.Runtime.CompilerServices;
using Symulator.Avalonia.Models;

namespace Symulator.Avalonia.ViewModels;

public sealed class LcdScreenViewModel : INotifyPropertyChanged
{
    private readonly LcdPixelBuffer _buffer;

    public LcdScreenViewModel(LcdPixelBuffer buffer)
    {
        _buffer = buffer;
        _buffer.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);
    }

    public bool[,] Pixels => _buffer.Pixels;
    public int FrameCount => _buffer.FrameCount;

    public double PixelScale { get; set; } = 3.0;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
