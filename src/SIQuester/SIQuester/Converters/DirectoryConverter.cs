using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Extracts directory path from full file path.
/// </summary>
[ValueConversion(typeof(string), typeof(string))]
public sealed class DirectoryConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value == null ? null : System.IO.Path.GetDirectoryName(value.ToString());

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
