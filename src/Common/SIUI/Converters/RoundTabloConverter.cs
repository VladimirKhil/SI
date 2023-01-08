using System.Globalization;
using System.Windows.Data;

namespace SIUI.Converters;

[ValueConversion(typeof(double), typeof(double))]
public sealed class RoundTabloConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Convert.ToBoolean(parameter) ? System.Convert.ToDouble(value) : -System.Convert.ToDouble(value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
