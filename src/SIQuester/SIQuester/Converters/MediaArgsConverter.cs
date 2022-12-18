using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class MediaArgsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Tuple.Create(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
