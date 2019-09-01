using System;
using System.Windows.Data;

namespace SIUI.Converters
{
    [ValueConversion(typeof(double), typeof(double))]
    public sealed class RoundTabloConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return System.Convert.ToBoolean(parameter) ? System.Convert.ToDouble(value) : -System.Convert.ToDouble(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
