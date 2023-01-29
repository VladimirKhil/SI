using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class PlusOneConverter: IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (int)value + 1;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
