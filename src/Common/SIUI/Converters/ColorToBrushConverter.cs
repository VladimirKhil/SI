using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIUI.Converters;

[ValueConversion(typeof(string), typeof(Brush))]
public sealed class ColorToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return null;
        }

        var colorString = (string)value;
        var color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(colorString);

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
