using System;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

/// <summary>
/// Compares value and parameter.
/// </summary>
public sealed class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Equals(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? parameter : throw new NotImplementedException();
}
