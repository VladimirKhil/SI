using System.Globalization;
using System.Windows.Data;

namespace Utils.Wpf.Converters;

/// <summary>
/// Converts an array of strings into a single string by calling <see cref="string.Join(string?, string?[])" />.
/// </summary>
[ValueConversion(typeof(string[]), typeof(string))]
public sealed class StringJoinConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.Join(", ", (string[])value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
