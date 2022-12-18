using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(int), typeof(bool))]
public sealed class GreaterThanValueConverter : IValueConverter
{
    public int BaseValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (int)value > BaseValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
