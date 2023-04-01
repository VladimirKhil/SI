using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SImulator.Converters;

/// <summary>
/// Makes object visible when target value is not null or empty.
/// </summary>
[ValueConversion(typeof(string), typeof(Visibility))]
public sealed class NotNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrEmpty((string)value) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
