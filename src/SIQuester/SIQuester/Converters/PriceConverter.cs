using SIPackages;
using SIQuester.Properties;
using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

/// <summary>
/// Converts question price to string representation.
/// </summary>
[ValueConversion(typeof(int), typeof(string))]
internal sealed class PriceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (int)value != Question.InvalidPrice ? value.ToString() : Resources.EmptyQuestion;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (string)value == Resources.EmptyQuestion ? Question.InvalidPrice : int.Parse((string)value);
}
