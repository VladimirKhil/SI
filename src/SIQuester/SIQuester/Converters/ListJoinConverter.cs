using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class ListJoinConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var result = new List<object>();

        foreach (var item in values)
        {
            if (item == DependencyProperty.UnsetValue)
            {
                continue;
            }

            result.AddRange((IEnumerable<object>)item);
        }

        return result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
