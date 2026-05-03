using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using NLog;

namespace Symulator.Avalonia.Controls;

public class RetroTextGridControl : Control
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static readonly StyledProperty<char[,]> CellsProperty =
        AvaloniaProperty.Register<RetroTextGridControl, char[,]>(nameof(Cells));

    public static readonly StyledProperty<int> RowsProperty =
        AvaloniaProperty.Register<RetroTextGridControl, int>(nameof(Rows), 24);

    public static readonly StyledProperty<int> ColumnsProperty =
        AvaloniaProperty.Register<RetroTextGridControl, int>(nameof(Columns), 40);

    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<RetroTextGridControl, double>(nameof(Scale), 1.0);

    public static readonly StyledProperty<Color> ForegroundColorProperty =
        AvaloniaProperty.Register<RetroTextGridControl, Color>(nameof(ForegroundColor), Color.Parse("#4DFF88"));

    public static readonly StyledProperty<Color> BackgroundColorProperty =
        AvaloniaProperty.Register<RetroTextGridControl, Color>(nameof(BackgroundColor), Color.Parse("#1a1a1a"));

    public char[,]? Cells { get => GetValue(CellsProperty); set => SetValue(CellsProperty, value); }
    public int Rows { get => GetValue(RowsProperty); set => SetValue(RowsProperty, value); }
    public int Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public double Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }
    public Color ForegroundColor { get => GetValue(ForegroundColorProperty); set => SetValue(ForegroundColorProperty, value); }
    public Color BackgroundColor { get => GetValue(BackgroundColorProperty); set => SetValue(BackgroundColorProperty, value); }

    private double _cellWidth = 10;
    private double _cellHeight = 18;
    private Typeface? _typeface;

    static RetroTextGridControl()
    {
        AffectsRender<RetroTextGridControl>(
            CellsProperty, RowsProperty, ColumnsProperty,
            ScaleProperty, ForegroundColorProperty, BackgroundColorProperty);
    }

    public RetroTextGridControl()
    {
        try
        {
            _typeface = new Typeface("avares://Symulator.Avalonia/Assets/Fonts/Apple1/#Apple1");
            Logger.Info("Apple-1 font typeface requested: {FontUri}", "avares://Symulator.Avalonia/Assets/Fonts/Apple1/#Apple1");
        }
        catch (Exception ex)
        {
            _typeface = new Typeface(FontFamily.Default);
            Logger.Warn(ex, "Apple-1 font failed to load, falling back to default font");
        }
    }

    protected override Size MeasureOverride(Size available)
    {
        return new Size(Columns * _cellWidth * Scale + 8, Rows * _cellHeight * Scale + 8);
    }

    public override void Render(DrawingContext context)
    {
        var fgBrush = new ImmutableSolidColorBrush(ForegroundColor);
        var bgBrush = new ImmutableSolidColorBrush(BackgroundColor);

        context.FillRectangle(bgBrush, new Rect(Bounds.Size));

        var cells = Cells;
        if (cells is null) return;

        var tf = _typeface ?? new Typeface(FontFamily.Default);
        double x0 = 4;
        double y0 = 4;
        double cw = _cellWidth * Scale;
        double ch = _cellHeight * Scale;
        double fontSize = 14 * Scale;

        for (int row = 0; row < Rows && row < cells.GetLength(0); row++)
        {
            for (int col = 0; col < Columns && col < cells.GetLength(1); col++)
            {
                char c = cells[row, col];
                if (c != ' ' && c != '\0')
                {
                    var pt = new Point(x0 + col * cw, y0 + row * ch);
                    var ft = new FormattedText(
                        c.ToString(),
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        tf,
                        fontSize,
                        fgBrush);
                    context.DrawText(ft, pt);
                }
            }
        }
    }
}
