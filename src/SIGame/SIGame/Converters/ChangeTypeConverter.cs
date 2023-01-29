using SIGame.Properties;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class ChangeTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? Resources.ChangeToComputer : Resources.ChangeToHuman;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
