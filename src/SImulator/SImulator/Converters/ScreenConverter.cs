using System;
using System.Globalization;
using System.Windows.Data;

namespace SImulator.Converters;

public sealed class ScreenConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value == System.Windows.Forms.Screen.PrimaryScreen ? "Основной монитор" : "Дополнительный монитор";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
