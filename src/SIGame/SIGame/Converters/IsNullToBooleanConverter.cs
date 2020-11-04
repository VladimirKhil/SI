using System;
using System.Windows.Data;

namespace SIGame.Converters
{
    [ValueConversion(typeof(object), typeof(bool))]
    public sealed class IsNullToBooleanConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            value == null;

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
