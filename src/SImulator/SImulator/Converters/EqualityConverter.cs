using System;
using System.Windows.Data;

namespace SImulator.Converters
{
    public sealed class EqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Equals(value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return parameter;
            else
                throw new NotImplementedException();
        }
    }
}
