using System;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace SIGame.Converters;

public sealed class EqualNumberToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        Equals(values[0], values[1]) ? Visibility.Visible : Visibility.Collapsed;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
