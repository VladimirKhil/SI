using System;
using System.Windows.Data;
using System.Windows;
using SICore;

namespace SIGame.Converters
{
    [ValueConversion(typeof(GameRole), typeof(Visibility))]
    public class IsPlayerConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null && (GameRole)value == GameRole.Player ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
