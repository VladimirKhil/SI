using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SIUI.Converters;

public sealed class PressedBottomRowHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var listSize = (int)value;
        var maxSize = Math.Min(4, listSize);

        return new GridLength(maxSize, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
