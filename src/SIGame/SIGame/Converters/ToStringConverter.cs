using System;
using System.Windows.Data;

namespace SIGame.Converters
{
    [ValueConversion(typeof(object), typeof(string))]
    public sealed class ToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null ? ((System.Windows.Input.Key)value).ToString() : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
