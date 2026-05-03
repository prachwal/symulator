using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Symulator.Machines.MinimalBlink.Views;

public class MinimalBlinkLcdControl : Control
{
    public static readonly StyledProperty<bool[,]> PixelsProperty =
        AvaloniaProperty.Register<MinimalBlinkLcdControl, bool[,]>(nameof(Pixels));

    public bool[,]? Pixels { get => GetValue(PixelsProperty); set => SetValue(PixelsProperty, value); }

    private const double Dot = 3.0;
    private const double Gap = 1.0;          // między kropkami
    private const double CharGap = 4.0;       // 1 kolumna odstępu (jak HD44780)
    private const double RowGap = 4.0;        // 1 wiersz odstępu między liniami
    private const double Margin = 4.0;
    private const double FrameW = 2.0;
    private const int CharW = 5;
    private const int CharH = 7;
    private const int Cols = 16;
    private const int Lines = 2;

    private static readonly Color FrameColor = Color.Parse("#2A3A2A");
    private static readonly Color BgColor = Color.Parse("#C0D9AF");
    private static readonly Color DotOnColor = Color.Parse("#3A6A5C");
    private static readonly Color DotOffColor = Color.Parse("#B0D0A0");

    static MinimalBlinkLcdControl()
    {
        AffectsRender<MinimalBlinkLcdControl>(PixelsProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PixelsProperty)
            InvalidateVisual();
    }

    protected override Size MeasureOverride(Size available)
    {
        double step = Dot + Gap;
        double cw = CharW * step - Gap + CharGap;
        double ch = CharH * step - Gap + RowGap;
        double w = Cols * cw - CharGap + Margin * 2 + FrameW * 2;
        double h = Lines * ch - RowGap + Margin * 2 + FrameW * 2;
        return new Size(w, h);
    }

    public override void Render(DrawingContext context)
    {
        var frameBrush = new ImmutableSolidColorBrush(FrameColor);
        var bgBrush = new ImmutableSolidColorBrush(BgColor);
        var dotOn = new ImmutableSolidColorBrush(DotOnColor);
        var dotOff = new ImmutableSolidColorBrush(DotOffColor);

        context.FillRectangle(frameBrush, new Rect(Bounds.Size));

        var inner = new Rect(FrameW, FrameW, Bounds.Width - FrameW * 2, Bounds.Height - FrameW * 2);
        context.FillRectangle(bgBrush, inner);

        var pixels = Pixels;
        if (pixels is null) return;

        double step = Dot + Gap;
        double radius = Dot * 0.2;

        for (int row = 0; row < Lines; row++)
        {
            double rowBaseY = Margin + FrameW + row * (CharH * step + RowGap);

            for (int col = 0; col < Cols; col++)
            {
                double colBaseX = Margin + FrameW + col * (CharW * step + CharGap);

                for (int py = 0; py < CharH; py++)
                {
                    int runStart = -1;
                    for (int px = 0; px <= CharW; px++)
                    {
                        int bx = col * CharW + px;
                        int by = row * CharH + py;
                        bool isOn = px < CharW
                            && bx < pixels.GetLength(0)
                            && by < pixels.GetLength(1)
                            && pixels[bx, by];

                        if (isOn && runStart < 0) runStart = px;
                        if (!isOn && runStart >= 0)
                        {
                            double x = colBaseX + runStart * step;
                            double y = rowBaseY + py * step;
                            double w = (px - runStart) * step + Dot;
                            context.FillRectangle(dotOn, new Rect(x, y, w, Dot), (float)radius);
                            runStart = -1;
                        }
                    }
                }
            }
        }
    }
}
