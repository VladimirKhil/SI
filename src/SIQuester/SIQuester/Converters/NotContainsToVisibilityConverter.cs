using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIQuester.Converters;

public sealed class NotContainsToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        ((ICollection<string>)values[0]).Contains((string)values[1]) ? Visibility.Collapsed : Visibility.Visible;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
