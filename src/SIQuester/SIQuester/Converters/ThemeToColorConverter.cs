using SIQuester.Model;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIQuester.Converters;

[ValueConversion(typeof(ThemeOption), typeof(Color))]
public sealed class ThemeToColorConverter : IValueConverter
{
    public Color LightColor { get; set; }
    public Color DarkColor { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ThemeOption theme)
        {
            return theme == ThemeOption.Dark ? DarkColor : LightColor;
        }

        return LightColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
