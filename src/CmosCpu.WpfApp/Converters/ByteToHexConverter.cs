using System.Globalization;
using System.Windows.Data;

namespace CmosCpu.WpfApp.Converters;

[ValueConversion(typeof(byte), typeof(string))]
public class ByteToHexConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte b)
            return $"0x{b:X2}";
        return "0x00";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
