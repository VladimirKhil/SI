using System.Globalization;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class MultiCostSetterConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var baseValue = values[0].ToString();
        var increment = values[1].ToString();

        return $"{baseValue} +{increment}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
