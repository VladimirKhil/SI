using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class UriConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        new Uri(value.ToString());

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
