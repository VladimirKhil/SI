using System;
using System.Windows.Data;

namespace SImulator.Converters
{
    public sealed class ScreenConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value == System.Windows.Forms.Screen.PrimaryScreen ? "Основной монитор" : "Дополнительный монитор";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
