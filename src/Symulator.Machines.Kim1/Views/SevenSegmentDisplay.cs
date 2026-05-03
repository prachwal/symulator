using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Symulator.Machines.Kim1.Views;

public class SevenSegmentDisplay : Control
{
    private static readonly Dictionary<char, byte> CharToSegments = new()
    {
        ['0'] = 0b01111110, ['1'] = 0b00110000, ['2'] = 0b01101101,
        ['3'] = 0b01111001, ['4'] = 0b00110011, ['5'] = 0b01011011,
        ['6'] = 0b01011111, ['7'] = 0b01110000, ['8'] = 0b01111111,
        ['9'] = 0b01111011, ['A'] = 0b01110111, ['b'] = 0b00011111,
        ['C'] = 0b01001110, ['c'] = 0b00001101, ['d'] = 0b00111101,
        ['E'] = 0b01001111, ['F'] = 0b01000111, ['g'] = 0b00011011,
        ['H'] = 0b00110111, ['h'] = 0b00010111, ['I'] = 0b00000110,
        ['J'] = 0b00111000, ['L'] = 0b00001110, ['n'] = 0b00010101,
        ['O'] = 0b00111110, ['o'] = 0b00011101, ['P'] = 0b01100111,
        ['r'] = 0b00000101, ['t'] = 0b00001111, ['U'] = 0b00111110,
        ['u'] = 0b00011100, ['Y'] = 0b00111011,
        ['-'] = 0b00000001, [' '] = 0b00000000, ['.'] = 0b10000000,
        ['_'] = 0b00001000, ['='] = 0b00001001, ['?'] = 0b01000111,
    };

    private static readonly Point[][] SegmentGeos =
    [
        // a: top horizontal
        [new(2,0), new(30,0), new(32,2), new(30,4), new(2,4), new(0,2)],
        // b: upper right vertical
        [new(30,2), new(32,4), new(32,20), new(30,22), new(28,20), new(28,4)],
        // c: lower right vertical
        [new(30,24), new(32,26), new(32,42), new(30,44), new(28,42), new(28,26)],
        // d: bottom horizontal
        [new(2,44), new(30,44), new(32,46), new(30,48), new(2,48), new(0,46)],
        // e: lower left vertical
        [new(0,26), new(2,24), new(4,26), new(4,42), new(2,44), new(0,42)],
        // f: upper left vertical
        [new(0,4), new(2,2), new(4,4), new(4,20), new(2,22), new(0,20)],
        // g: center horizontal
        [new(2,23), new(30,23), new(32,25), new(30,27), new(2,27), new(0,25)],
        // dp: decimal point lower right
        [new(32,46), new(36,46), new(36,50), new(32,50)],
    ];

    private readonly Dictionary<(int Digit, int Seg), StreamGeometry> _geoCache = [];

    private static readonly IBrush OnColor = new ImmutableSolidColorBrush(Color.Parse("#4DFF88"));
    private static readonly IBrush OffColor = new ImmutableSolidColorBrush(Color.Parse("#1a3a2a"));
    private static readonly IBrush OffDpColor = new ImmutableSolidColorBrush(Color.Parse("#0a1a10"));

    public static readonly StyledProperty<string> DigitsProperty =
        AvaloniaProperty.Register<SevenSegmentDisplay, string>(nameof(Digits), "------");

    public string Digits
    {
        get => GetValue(DigitsProperty);
        set => SetValue(DigitsProperty, value);
    }

    static SevenSegmentDisplay()
    {
        AffectsRender<SevenSegmentDisplay>(DigitsProperty);
    }

    protected override Size MeasureOverride(Size available) => new(260, 56);
    protected override Size ArrangeOverride(Size final) => new(260, 56);

    public override void Render(DrawingContext context)
    {
        string digits = Digits ?? "------";
        int spacing = 44;
        int offsetX = 4;
        int offsetY = 4;

        for (int d = 0; d < 6 && d < digits.Length; d++)
        {
            byte segMask = CharToSegments.TryGetValue(digits[d], out var mask) ? mask : (byte)0;
            int dx = offsetX + d * spacing;

            for (int s = 0; s < 7; s++)
            {
                bool on = (segMask & (1 << (6 - s))) != 0;
                DrawSegment(context, d, s, dx, offsetY, on ? OnColor : OffColor);
            }

            bool dpOn = (segMask & 0x80) != 0;
            DrawSegment(context, d, 7, dx, offsetY, dpOn ? OnColor : OffDpColor);
        }
    }

    private void DrawSegment(DrawingContext context, int digit, int seg, int dx, int dy, IBrush brush)
    {
        if (!_geoCache.TryGetValue((digit, seg), out var geo))
        {
            var points = SegmentGeos[seg];
            var tg = new StreamGeometry();
            using var ctx = tg.Open();
            ctx.BeginFigure(new Point(points[0].X + digit * 44 + 4, points[0].Y + 4), true);
            for (int i = 1; i < points.Length; i++)
                ctx.LineTo(new Point(points[i].X + digit * 44 + 4, points[i].Y + 4));
            ctx.EndFigure(true);
            geo = tg;
            _geoCache[(digit, seg)] = tg;
        }
        context.DrawGeometry(brush, null, geo);
    }
}
