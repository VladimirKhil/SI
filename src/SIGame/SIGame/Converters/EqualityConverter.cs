using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

[ValueConversion(typeof(string), typeof(bool))]
public sealed class EqualityConverter : IValueConverter, IMultiValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Equals(value, parameter) || value?.ToString() == parameter?.ToString();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        Equals(values[0], values[1]);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? System.Convert.ToInt32(parameter) : (object)-1;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
