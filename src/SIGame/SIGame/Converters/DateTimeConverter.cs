using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class DateTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var date = (DateTime)value;
        return date == DateTime.MinValue ? "" : date.ToString(culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
