using System.Globalization;
using System.Windows.Data;

namespace SIUI.Converters;

[ValueConversion(typeof(double), typeof(double))]
public sealed class Adder : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return null;
        }

        return System.Convert.ToDouble(value) + System.Convert.ToDouble(parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
