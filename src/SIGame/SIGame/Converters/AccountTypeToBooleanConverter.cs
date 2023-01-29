using SIGame.ViewModel;
using System;
using System.Globalization;
using System.Windows.Data;

namespace SIGame.Converters;

public sealed class AccountTypeToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (AccountTypes)value == AccountTypes.Computer;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? AccountTypes.Computer : AccountTypes.Human;
}
