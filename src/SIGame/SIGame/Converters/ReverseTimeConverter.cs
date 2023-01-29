using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class ReverseTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => 100.0 - (double)value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
