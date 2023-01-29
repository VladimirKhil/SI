using SIData;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

[ValueConversion(typeof(GameRole), typeof(bool))]
public sealed class IsRoleConverter : IValueConverter
{
    public GameRole Role { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (GameRole)value == Role;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
