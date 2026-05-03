using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using CmosCpu.Computer.Devices;

namespace Symulator.Avalonia.Models;

public sealed class LcdPixelBuffer : INotifyPropertyChanged
{
    public const int CharsPerLine = 16;
    public const int Lines = 2;
    public const int CharWidth = 5;
    public const int CharHeight = 7;
    public const int TotalWidth = CharsPerLine * CharWidth;   // 80
    public const int TotalHeight = Lines * CharHeight;        // 14
    public const int TotalPixels = TotalWidth * TotalHeight;   // 1120

    private readonly bool[,] _pixels = new bool[TotalWidth, TotalHeight];
    private long _frameCounter;
    private bool _needsInvalidate;

    public bool[,] Pixels => _pixels;
    public int FrameCount => (int)(_frameCounter & 0x7FFFFFFF);
    public long FrameCounter => _frameCounter;

    // Attach to Hd44780Lcd device
    public void AttachToDevice(Hd44780LcdAccess lcdAccess)
    {
        lcdAccess.DisplayChanged += OnDisplayChanged;
    }

    private void OnDisplayChanged(Hd44780LcdAccess lcdAccess)
    {
        RenderFrame(lcdAccess);
        _needsInvalidate = true;
        Dispatcher.UIThread.Post(() =>
        {
            if (_needsInvalidate)
            {
                _needsInvalidate = false;
                OnPropertyChanged(nameof(Pixels));
                OnPropertyChanged(nameof(FrameCount));
            }
        }, DispatcherPriority.Render);
    }

    private void RenderFrame(Hd44780LcdAccess lcdAccess)
    {
        Array.Clear(_pixels, 0, _pixels.Length);

        for (int row = 0; row < Lines; row++)
        {
            for (int col = 0; col < CharsPerLine; col++)
            {
                int ddramAddr = row == 0 ? col : 0x40 + col;
                byte charCode = ddramAddr < lcdAccess.Ddram.Length
                    ? lcdAccess.Ddram[ddramAddr]
                    : (byte)0x20;

                RenderChar(charCode, col, row, lcdAccess);
            }
        }

        // Cursor underline (with blink)
        if (lcdAccess.CursorOn && lcdAccess.DisplayOn)
        {
            bool blinkOff = lcdAccess.BlinkOn && (_frameCounter & 0x0F) >= 8;
            if (!blinkOff)
            {
                int cx = lcdAccess.CursorCol * CharWidth;
                int cy = lcdAccess.CursorRow * CharHeight + (CharHeight - 1);
                if (cy < TotalHeight)
                    for (int x = cx; x < cx + CharWidth && x < TotalWidth; x++)
                        _pixels[x, cy] = true;
            }
        }

        _frameCounter++;
    }

    private void RenderChar(byte charCode, int col, int row, Hd44780LcdAccess lcdAccess)
    {
        int baseX = col * CharWidth;
        int baseY = row * CharHeight;

        byte[] pattern;
        if (charCode < 0x08 && lcdAccess.Cgram is not null)
        {
            // Custom CGRAM character
            int cgramAddr = charCode * 7;
            pattern = new byte[7];
            for (int i = 0; i < 7 && cgramAddr + i < lcdAccess.Cgram.Length; i++)
                pattern[i] = lcdAccess.Cgram[cgramAddr + i];
        }
        else
        {
            pattern = Data.CgromTable.GetPattern(charCode);
        }

        for (int py = 0; py < 7 && py < pattern.Length; py++)
        {
            byte rowBits = pattern[py];
            for (int px = 0; px < CharWidth; px++)
            {
                int bx = baseX + px;
                int by = baseY + py;
                if (bx < TotalWidth && by < TotalHeight)
                    _pixels[bx, by] = (rowBits >> (4 - px) & 1) != 0;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

/// <summary>
/// Thread-safe snapshot of Hd44780Lcd state for render pipeline.
/// Copied from the device on the emulator thread; consumed on the UI thread.
/// </summary>
public sealed class Hd44780LcdAccess
{
    private readonly CmosCpu.Computer.Devices.Hd44780Lcd _device;
    private readonly byte[] _ddram = new byte[CmosCpu.Computer.Devices.Hd44780Lcd.DdramSize];
    private readonly byte[] _cgram = new byte[CmosCpu.Computer.Devices.Hd44780Lcd.CgramSize];

    public byte[] Ddram => _ddram;
    public byte[]? Cgram => _cgram;
    public bool DisplayOn { get; private set; }
    public bool CursorOn { get; private set; }
    public bool BlinkOn { get; private set; }
    public int CursorRow { get; private set; }
    public int CursorCol { get; private set; }

    public event Action<Hd44780LcdAccess>? DisplayChanged;

    public Hd44780LcdAccess(Hd44780Lcd device)
    {
        _device = device;
        device.DisplayChanged += OnDeviceChanged;
    }

    private void OnDeviceChanged(Hd44780Lcd device)
    {
        // Snapshot state atomically
        Array.Copy(device.Ddram, _ddram, _ddram.Length);
        Array.Copy(device.Cgram, _cgram, _cgram.Length);
        DisplayOn = device.DisplayOn;
        CursorOn = device.CursorOn;
        BlinkOn = device.BlinkOn;
        CursorRow = device.CursorRow;
        CursorCol = device.CursorCol;

        DisplayChanged?.Invoke(this);
    }
}
