using System.Globalization;
using System.Windows.Data;

namespace CmosCpu.WpfApp.Converters;

[ValueConversion(typeof(ushort), typeof(string))]
public class UShortToHexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ushort u)
            return $"0x{u:X4}";
        return "0x0000";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
