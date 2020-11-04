using System;
using System.Windows.Data;
using System.Windows;

namespace SIWindows.Converters
{
    [ValueConversion(typeof(bool), typeof(WindowStyle))]
    public sealed class BooleanToWindowStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return WindowStyle.SingleBorderWindow;

            var b = bool.Parse(value.ToString());
            return b ? WindowStyle.None : WindowStyle.SingleBorderWindow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
