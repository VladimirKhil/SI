using SIGame.ViewModel;
using System;
using System.Windows.Data;

namespace SIGame.Converters
{
    public sealed class AccountTypeToBooleanConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (AccountTypes)value == AccountTypes.Computer;

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            (bool)value ? AccountTypes.Computer : AccountTypes.Human;
    }
}
