using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using NLog;
using Symulator.Avalonia.Models;

namespace Symulator.Avalonia.Controls;

public class LcdScreenControl : Control
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // --- Bindable properties ---

    public static readonly StyledProperty<bool[,]> PixelsProperty =
        AvaloniaProperty.Register<LcdScreenControl, bool[,]>(nameof(Pixels));

    public static readonly StyledProperty<double> PixelScaleProperty =
        AvaloniaProperty.Register<LcdScreenControl, double>(nameof(PixelScale), 3.0);

    public bool[,]? Pixels { get => GetValue(PixelsProperty); set => SetValue(PixelsProperty, value); }
    public double PixelScale { get => GetValue(PixelScaleProperty); set => SetValue(PixelScaleProperty, value); }

    public static readonly StyledProperty<Color> PixelOnColorProperty =
        AvaloniaProperty.Register<LcdScreenControl, Color>(nameof(PixelOnColor), Color.Parse("#3A6A5C"));

    public static readonly StyledProperty<Color> PixelOffColorProperty =
        AvaloniaProperty.Register<LcdScreenControl, Color>(nameof(PixelOffColor), Color.Parse("#B0D0A0"));

    public static readonly StyledProperty<Color> BackgroundColorProperty =
        AvaloniaProperty.Register<LcdScreenControl, Color>(nameof(BackgroundColor), Color.Parse("#C0D9AF"));

    public static readonly StyledProperty<Color> FrameColorProperty =
        AvaloniaProperty.Register<LcdScreenControl, Color>(nameof(FrameColor), Color.Parse("#2A3A2A"));

    public Color PixelOnColor { get => GetValue(PixelOnColorProperty); set => SetValue(PixelOnColorProperty, value); }
    public Color PixelOffColor { get => GetValue(PixelOffColorProperty); set => SetValue(PixelOffColorProperty, value); }
    public Color BackgroundColor { get => GetValue(BackgroundColorProperty); set => SetValue(BackgroundColorProperty, value); }
    public Color FrameColor { get => GetValue(FrameColorProperty); set => SetValue(FrameColorProperty, value); }

    // --- Cached brushes ---

    private IBrush? _cachedDotOn;
    private IBrush? _cachedGradientBg;
    private IBrush? _cachedFrame;
    private Color _lastOn;
    private Color _lastBg;
    private Color _lastFrame;

    // --- Layout constants ---

    private const double Gap = 1.0;          // między pikselami w znaku
    private const double CharGap = 4.0;       // 1 kolumna odstępu (jak HD44780)
    private const double RowGap = 4.0;        // 1 wiersz odstępu między liniami
    private new const double Margin = 6.0;     // margines wewnątrz ramki
    private const double FrameWidth = 3.0;     // grubość ramki

    private IBrush GetDotOnBrush()
    {
        if (_cachedDotOn is null || _lastOn != PixelOnColor)
        {
            _cachedDotOn = new ImmutableSolidColorBrush(PixelOnColor);
            _lastOn = PixelOnColor;
        }
        return _cachedDotOn;
    }

    private IBrush GetGradientBgBrush()
    {
        if (_cachedGradientBg is null || _lastBg != BackgroundColor)
        {
            var c = BackgroundColor;
            byte r = (byte)Math.Min(c.R + 25, 255);
            byte g = (byte)Math.Min(c.G + 20, 255);
            byte b = (byte)Math.Min(c.B + 15, 255);
            var lighter = Color.FromRgb(r, g, b);
            _cachedGradientBg = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop { Color = lighter, Offset = 0 },
                    new GradientStop { Color = c, Offset = 1 }
                }
            };
            _lastBg = BackgroundColor;
        }
        return _cachedGradientBg;
    }

    private IBrush GetFrameBrush()
    {
        if (_cachedFrame is null || _lastFrame != FrameColor)
        {
            _cachedFrame = new ImmutableSolidColorBrush(FrameColor);
            _lastFrame = FrameColor;
        }
        return _cachedFrame;
    }

    static LcdScreenControl()
    {
        AffectsRender<LcdScreenControl>(
            PixelsProperty, PixelScaleProperty,
            PixelOnColorProperty, PixelOffColorProperty,
            BackgroundColorProperty, FrameColorProperty);
    }

    protected override Size MeasureOverride(Size available)
    {
        double dot = PixelScale;
        double cw = LcdPixelBuffer.CharWidth * (dot + Gap) - Gap + CharGap;
        double ch = LcdPixelBuffer.CharHeight * (dot + Gap) - Gap + RowGap;
        double w = LcdPixelBuffer.CharsPerLine * cw - CharGap + Margin * 2 + FrameWidth * 2;
        double h = LcdPixelBuffer.Lines * ch - RowGap + Margin * 2 + FrameWidth * 2;
        return new Size(w, h);
    }

    public override void Render(DrawingContext context)
    {
        double fw = FrameWidth;
        var frameBrush = GetFrameBrush();
        var gradBg = GetGradientBgBrush();
        var dotOn = GetDotOnBrush();

        // 1. Ramka zewnętrzna
        context.FillRectangle(frameBrush, new Rect(Bounds.Size));

        // 2. Obszar wewnętrzny z gradientem
        var innerRect = new Rect(fw, fw, Bounds.Width - fw * 2, Bounds.Height - fw * 2);
        context.FillRectangle(gradBg, innerRect);

        // 3. Wewnętrzna cienka ramka (bezel shadow)
        var shadowBrush = new ImmutableSolidColorBrush(Color.FromArgb(40, 0, 0, 0));
        context.FillRectangle(shadowBrush, new Rect(fw + 1, fw + 1, innerRect.Width - 2, innerRect.Height - 2));

        // 4. Segmenty LCD
        var pixels = Pixels;
        if (pixels is null) return;

        double dot = PixelScale;
        double step = dot + Gap;
        double radius = dot * 0.2;

        for (int row = 0; row < LcdPixelBuffer.Lines; row++)
        {
            double rowBaseY = Margin + fw + row * (LcdPixelBuffer.CharHeight * step + RowGap);

            for (int col = 0; col < LcdPixelBuffer.CharsPerLine; col++)
            {
                double colBaseX = Margin + fw + col * (LcdPixelBuffer.CharWidth * step + CharGap);

                // Run-length render: group consecutive ON pixels per row within a character
                for (int py = 0; py < LcdPixelBuffer.CharHeight; py++)
                {
                    int runStart = -1;

                    for (int px = 0; px <= LcdPixelBuffer.CharWidth; px++)
                    {
                        int bx = col * LcdPixelBuffer.CharWidth + px;
                        int by = row * LcdPixelBuffer.CharHeight + py;

                        bool isOn = px < LcdPixelBuffer.CharWidth
                            && bx < pixels.GetLength(0) && by < pixels.GetLength(1)
                            && pixels[bx, by];

                        if (isOn && runStart < 0)
                            runStart = px;

                        if (!isOn && runStart >= 0)
                        {
                            double x = colBaseX + runStart * step;
                            double y = rowBaseY + py * step;
                            double w = (px - runStart) * step + dot;
                            context.FillRectangle(dotOn, new Rect(x, y, w, dot), (float)radius);
                            runStart = -1;
                        }
                    }
                }
            }
        }
    }
}
