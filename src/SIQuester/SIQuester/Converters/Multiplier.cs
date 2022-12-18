using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class Multiplier : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
