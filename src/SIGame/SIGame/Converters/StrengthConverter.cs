using SIData;
using SIGame.Properties;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

[ValueConversion(typeof(ComputerAccount), typeof(string))]
public sealed class StrengthConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ComputerAccount account)
        {
            return null;
        }

        return account.F switch
        {
            > 170 => Resources.Strength_High,
            > 130 => Resources.Strength_Middle,
            > 10 => Resources.Strength_Low,
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
