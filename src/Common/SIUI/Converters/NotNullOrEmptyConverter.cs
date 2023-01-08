using System.Globalization;
using System.Windows.Data;

namespace SIUI.Converters;

/// <summary>
/// Checks if the string value is not null and not empty.
/// </summary>
[ValueConversion(typeof(string), typeof(bool))]
public sealed class NotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        !string.IsNullOrEmpty(value?.ToString());

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
