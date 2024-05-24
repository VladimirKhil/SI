using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Converts collection to a string with comma separated items.
/// </summary>
[ValueConversion(typeof(IEnumerable<string>), typeof(string))]
public sealed class CollectionConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is IEnumerable<string> enumerable ? string.Join(", ", enumerable) : value?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
