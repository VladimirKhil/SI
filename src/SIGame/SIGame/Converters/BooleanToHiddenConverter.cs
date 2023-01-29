using System;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace SIGame.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BooleanToHiddenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (value == null || !(bool)value) ? Visibility.Hidden : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
