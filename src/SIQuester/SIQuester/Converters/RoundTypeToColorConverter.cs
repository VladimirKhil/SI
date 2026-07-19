using SIPackages.Core;
using SIQuester.Model;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIQuester.Converters;

[ValueConversion(typeof(object[]), typeof(Brush))]
internal sealed class RoundTypeToColorConverter : IMultiValueConverter
{
    public Brush? CommonBrush { get; set; }

    public Brush? CommonBrushLight { get; set; }

    public Brush? CommonBrushDark { get; set; }

    public Brush? CommonBrushDarkGray { get; set; }

    public Brush? FinalBrush { get; set; }

    private Brush? GetThemeAwareCommonBrush(ThemeOption theme)
    {
        return theme switch
        {
            ThemeOption.Light => CommonBrushLight ?? CommonBrush,
            ThemeOption.Dark => CommonBrushDark ?? CommonBrush,
            ThemeOption.DarkGray => CommonBrushDarkGray ?? CommonBrush,
            _ => CommonBrush
        };
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string roundType)
        {
            return CommonBrush ?? Brushes.Black;
        }

        var theme = values.Length > 1 && values[1] is ThemeOption themeOption ? themeOption : ThemeOption.Light;

        return roundType switch
        {
            RoundTypes.Final => FinalBrush ?? Brushes.Black,
            _ => GetThemeAwareCommonBrush(theme) ?? Brushes.Black
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
