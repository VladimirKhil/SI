using SIGame.Properties;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

[ValueConversion(typeof(string), typeof(string))]
public sealed class StakeHeaderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value == null ? Resources.Stake : $"{Resources.Stake} ({value})";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
