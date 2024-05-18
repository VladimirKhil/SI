using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

[ValueConversion(typeof(ICollection<string>), typeof(bool))]
public sealed class ContainsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        ((ICollection<string>)values[0]).Contains((string)values[1]);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
