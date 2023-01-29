using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class SpeedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        10 - (int)value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        10 - System.Convert.ToInt32(value);
}
