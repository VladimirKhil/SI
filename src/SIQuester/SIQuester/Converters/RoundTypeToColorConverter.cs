using SIPackages.Core;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIQuester.Converters;

internal sealed class RoundTypeToColorConverter : IValueConverter
{
    public Brush? CommonBrush { get; set; }

    public Brush? FinalBrush { get; set; }

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (string)value switch
        {
            RoundTypes.Final => FinalBrush,
            _ => CommonBrush
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
