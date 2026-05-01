using System.Globalization;
using System.Windows.Data;

namespace CmosCpu.WpfApp.Converters;

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToOnOffConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? "ON" : "OFF";
        return "OFF";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == "ON";
    }
}
