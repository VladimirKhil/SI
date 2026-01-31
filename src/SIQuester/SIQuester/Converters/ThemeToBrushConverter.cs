using SIQuester.Model;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIQuester.Converters;

[ValueConversion(typeof(ThemeOption), typeof(Brush))]
public sealed class ThemeToBrushConverter : IValueConverter
{
    public Brush? LightBrush { get; set; }
    public Brush? DarkBrush { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ThemeOption theme)
        {
            return theme == ThemeOption.Dark ? DarkBrush ?? Brushes.Black : LightBrush ?? Brushes.White;
        }

        return LightBrush ?? Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
