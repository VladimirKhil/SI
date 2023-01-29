using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SIGame.Converters;

public sealed class RequiredToColorConverter : IValueConverter
{
    public Brush ErrorBrush { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var count = (int)value;
        return count > 0 ? System.Windows.SystemColors.ControlLightBrush : ErrorBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
