using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class CollectionConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.Join(", ", (IEnumerable<string>)value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
