using System.Globalization;
using System.Windows.Data;

namespace SIUI.Converters;

[ValueConversion(typeof(double), typeof(double))]
public sealed class Multiplier : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return null;
        }

        if (!double.TryParse(value.ToString(), out double arg1))
        {
            return null;
        }

        if (!double.TryParse(parameter.ToString()?.Replace('.', ','), out double arg2)) // TODO: Use Culture
        {
            return null;
        }

        return arg1 * arg2;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
