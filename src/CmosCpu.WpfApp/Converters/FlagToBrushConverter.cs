using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CmosCpu.WpfApp.Converters;

[ValueConversion(typeof(bool), typeof(Brush))]
public class FlagToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x00));
        return new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
