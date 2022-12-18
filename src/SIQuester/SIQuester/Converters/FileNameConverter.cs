using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class FileNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value == null ? null : System.IO.Path.GetFileName(value.ToString());

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
