using System;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace SIGame.Converters;

public sealed class IsBeforeGameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter == null)
        {
            return Visibility.Hidden;
        }

        var show = System.Convert.ToBoolean(parameter);
        return (System.Convert.ToBoolean(value) ^ show) ? Visibility.Visible : Visibility.Hidden;            
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
