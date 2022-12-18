using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(object), typeof(bool))]
public sealed class NotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value != null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
